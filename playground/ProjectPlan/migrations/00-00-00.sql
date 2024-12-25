CREATE OR REPLACE FUNCTION system.as_uuid("input" TEXT)
RETURNS UUID
LANGUAGE sql AS
$$
SELECT 
    CASE 
        WHEN $1 ~* '^[A-F0-9]{8}-?[A-F0-9]{4}-?[A-F0-9]{4}-?[A-F0-9]{4}-?[A-F0-9]{12}$' THEN 
            "input"::UUID
        ELSE NULL
    END
$$;

CREATE OR REPLACE FUNCTION system.as_int("input" TEXT)
RETURNS INTEGER
LANGUAGE sql AS
$$
SELECT 
    CASE 
        WHEN $1 ~ '^[-]?[0-9]{1,10}$' THEN 
            "input"::INTEGER
        ELSE NULL
    END
$$;

CREATE OR REPLACE FUNCTION system.as_numeric("input" TEXT)
RETURNS NUMERIC
LANGUAGE sql AS
$$
SELECT 
    CASE 
        WHEN $1 ~ '^[-]?[0-9]+(\.[0-9]+)?$' THEN 
            "input"::NUMERIC
        ELSE NULL
    END
$$;


CREATE TABLE system.gc_object 
(
	object_id UUID PRIMARY KEY DEFAULT(extensions.gen_random_uuid()), 
    created_on timestamp with time zone NOT NULL DEFAULT now(),
    deleted_on timestamp with time zone,
	CONSTRAINT is_abstract CHECK(false) NO INHERIT
);



CREATE TYPE api.http_request AS
(
	"method" text,
	address text,
	headers extensions.hstore,
	"content" jsonb
);


CREATE TYPE api.http_response AS
(
	status_code int,
	headers extensions.hstore,
	"content" jsonb
);


CREATE TABLE api.http_route
(
	PRIMARY KEY(object_id),
	sequence_number BIGINT GENERATED ALWAYS AS IDENTITY,	
	version_regexp text NOT NULL DEFAULT('^'),
	method_regexp text NOT NULL DEFAULT('^get$'),
	address_regexp text NOT NULL,
	"handler" text NOT NULL
) INHERITS(system.gc_object);
CREATE INDEX ix_httproute_sequencenumber ON api.http_route(sequence_number)
INCLUDE(address_regexp, version_regexp, method_regexp)
WHERE deleted_on IS NULL;


CREATE OR REPLACE FUNCTION api.pvw_http_route("address" text, "method" text, "version" text) 
RETURNS TABLE("handler" text, sequence_number bigint, object_id uuid)
AS
$$
	SELECT 
		c_route.handler,
		c_route.sequence_number,
		c_route.object_id
	FROM
		api.http_route AS c_route
	WHERE
		c_route.deleted_on is null		
	AND $1 ~* c_route.address_regexp
	AND (CASE WHEN "method"  IS NULL THEN true ELSE $2 ~* c_route.method_regexp END)
	AND (CASE WHEN "version" IS NULL THEN true ELSE $3 ~* c_route.version_regexp END)
	ORDER BY sequence_number DESC;
$$ LANGUAGE 'sql';



CREATE OR REPLACE FUNCTION api.check_http_route()
RETURNS TRIGGER AS $$
DECLARE 
  test TEXT;
BEGIN
  BEGIN
    -- Check if the regular expressions are valid by trying to match a string
    test := REGEXP_REPLACE('test', NEW.version_regexp, '');
    test := REGEXP_REPLACE('test', NEW.method_regexp, '');
    test := REGEXP_REPLACE('test', NEW.address_regexp, '');
  EXCEPTION WHEN others THEN
    RAISE EXCEPTION 'Invalid regular expression in version_regexp, method_regexp, or url_regexp.';
  END;

  -- Check if the function exists in the 'api' schema
  IF NOT EXISTS (
    SELECT 1 
    FROM pg_proc p
    JOIN pg_namespace n ON p.pronamespace = n.oid 
    WHERE n.nspname = 'api' AND p.proname = NEW.handler
  ) THEN
    RAISE EXCEPTION 'Function named "%" does not exist in the "api" schema.', NEW.handler;
  END IF;
  
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;


CREATE TRIGGER check_http_route
BEFORE INSERT OR UPDATE ON api.http_route
FOR EACH ROW EXECUTE FUNCTION api.check_http_route();


CREATE OR REPLACE FUNCTION api.http_request_build(
	"method" text,
	address text,
	headers extensions.hstore,
	"content" jsonb DEFAULT('{}')
) RETURNS api.http_request AS $$
SELECT ROW($1, $2, $3, $4)::api.http_request;
$$ LANGUAGE sql;


CREATE OR REPLACE FUNCTION api.http_response_build(
    status_code int,
    headers extensions.hstore,
    "content" jsonb
) RETURNS api.http_response AS $$
SELECT ROW($1, $2, $3)::api.http_response;
$$ LANGUAGE sql;


CREATE OR REPLACE FUNCTION api.http_response_build(
    status_code int,
    headers extensions.hstore,
    "message" text
) RETURNS api.http_response AS $$
SELECT ROW($1, $2, jsonb_build_object('message', $3))::api.http_response;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION api.http_invoke(
	"method" text,
	address text, 
	headers extensions.hstore,
	content jsonb DEFAULT('{}'::JSONB)) RETURNS api.http_response
AS
$body$
DECLARE
	v_context record;
	v_response api.http_response;
	v_std_headers extensions.hstore := 'Current-Version=>1.0, Content-Type=>application/json';
	v_sql text;
BEGIN
	SELECT
		c_request.method,
		c_request.address,
		c_request.headers,
		c_request.content,
		c_request.user_id,
		c_request.company_id,
		c_request.client_version,
		c_company.object_id AS company_object_id,
		c_company.name AS company_name,
		c_user.object_id AS user_object_id,
		c_route.handler AS "handler"
	INTO v_context
	FROM 
	(
		SELECT
			TRIM($1) AS "method",
			TRIM($2) AS "address",
			$3 AS "headers",
			$4 AS "content",
			TRIM(headers->'USER-ID') AS user_id,
			TRIM(headers->'COMPANY-ID') AS company_id,
			SUBSTRING(address FROM '(?i)(?:(?:api-?)?version|v)=(\S+)') AS client_version
	) AS c_request
	LEFT JOIN data.company AS c_company 
		ON c_company.id = c_request.company_id 
		AND c_company.deleted_on IS NULL
	LEFT JOIN data.user AS c_user 
		ON c_user.id = c_request.user_id
		AND c_user.company_object_id = c_company.object_id
		AND c_user.deleted_on IS NULL
	LEFT JOIN LATERAL
	(
		SELECT c_match.handler
		FROM api.pvw_http_route(c_request.address, c_request.method, c_request.client_version) AS c_match
		WHERE 
			c_user.id IS NOT NULL
		AND c_request.client_version IS NOT NULL
		ORDER BY 
			c_match.sequence_number DESC
		LIMIT 1
	) AS c_route 
		ON c_user.deleted_on IS NULL;
		
	--RAISE NOTICE '%', to_json(v_context);
		
	IF v_context.handler IS NOT NULL THEN		
		SELECT FORMAT($$ SELECT * FROM api.%s(api.http_request_build($1, $2, $3, $4)); $$, v_context.handler) 
		INTO v_sql;
		--RAISE NOTICE 'SQL: %', v_sql;
		headers := headers||
			extensions.hstore('USER-ID', v_context.user_object_id::text) ||
			extensions.hstore('COMPANY-ID', v_context.company_object_id::text);
		EXECUTE v_sql INTO v_response USING v_context.method, v_context.address, headers, v_context.content;
		v_response.headers := COALESCE(v_response.headers, v_std_headers)||v_std_headers;
		RETURN v_response;
	END IF;
	

	IF v_context.user_id IS NULL THEN
		RETURN api.http_response_build(401, v_std_headers, 
		'Unauthorized. Valid credentials are required to access this resource. USER-ID header is missing.');
	END IF;
	
	IF v_context.company_id IS NULL THEN
		RETURN api.http_response_build(401, v_std_headers, 
		'Unauthorized. Valid credentials are required to access this resource. COMPANY-ID header is missing.');
	END IF;
	
	IF v_context.company_object_id IS NULL THEN
		RETURN api.http_response_build(401, v_std_headers, 
		'Unauthorized. The user not found.');
	END IF;
	
	IF v_context.client_version IS NULL THEN
		RETURN api.http_response_build(400, v_std_headers, 
		'Bad Request. The client version is missing from the request.');
	END IF;
	
	
	
	IF EXISTS (SELECT * FROM api.pvw_http_route(address, v_context.method, NULL)) THEN
		RETURN api.http_response_build(400, v_std_headers, 
		FORMAT('Bad Request. Version %s is not supported for this endpoint.', quote_literal(v_context.client_version)));
	END IF;

	IF EXISTS (SELECT * FROM api.pvw_http_route(address, null, v_context.client_version)) THEN
		RETURN api.http_response_build(400, v_std_headers, 
		FORMAT('Method Not Allowed. The %s method is not allowed for this endpoint.', UPPER(quote_literal($1))));
	END IF;

	RETURN api.http_response_build(404, v_std_headers, 
		'Not Found. The requested resource could not be located.');

END;
$body$ LANGUAGE 'plpgsql';