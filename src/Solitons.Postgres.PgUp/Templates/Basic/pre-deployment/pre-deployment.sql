/*
  This template establishes foundational schemas, extensions, and roles to accelerate project initiation. 
  Adapt and extend as necessary to align with project requirements.

  This script is pre-configured for optimal setup. Adjust schema designs, roles, and migration tracking as needed for project-specific intricacies.

  Maintain script order and clarity in the pre-deployment directory. 
  Tailor 'pgup.json' for precise control over script execution.
*/



RESET ROLE; -- The following command require the current session role


-- Schema for holding PostgreSQL extensions, keeping them separate from business data.
CREATE SCHEMA IF NOT EXISTS "extensions" AUTHORIZATION ${databaseOwner};
COMMENT ON SCHEMA "extensions" IS 'Separates PostgreSQL extensions from business data for cleaner management.';

-- Create the required extensions within the "extensions" schema.
CREATE EXTENSION IF NOT EXISTS hstore SCHEMA extensions CASCADE;
CREATE EXTENSION IF NOT EXISTS pgcrypto SCHEMA extensions CASCADE;
CREATE EXTENSION IF NOT EXISTS pg_trgm SCHEMA extensions CASCADE;

-- Ensure the existence of the database manager role and grant necessary privileges.

DO $$ 
BEGIN 
    -- Check if the database manager role exists, and create it if not.
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = '${databaseAdmin}') THEN
        CREATE ROLE ${databaseAdmin} LOGIN PASSWORD '${databaseAdminPassword}';
	ELSE
	    -- If the role exists, update its password and connection limit.
		ALTER ROLE ${databaseAdmin} WITH 
			PASSWORD '${databaseAdminPassword}'
			CONNECTION LIMIT 5;
    END IF;
	
	-- Add a comment to describe the purpose of the database manager role.
	COMMENT ON ROLE ${databaseAdmin} IS 'Manages database operations for ${databaseName}, with specified login credentials.';
	
	-- Grant the database owner role to the database manager.
	GRANT ${databaseOwner} TO ${databaseAdmin};
END $$;



ALTER SCHEMA "public" OWNER TO ${databaseOwner};


-- Create schemas to organize database objects effectively.
SET ROLE ${databaseOwner};


-- Set schema ownership for consistency.
ALTER SCHEMA "extensions" OWNER TO ${databaseOwner};

-- Set the search path to prioritize schema lookup.
ALTER DATABASE ${databaseName} SET search_path TO "public", "extensions";

-- Create a table to track executed migration scripts.


CREATE TABLE IF NOT EXISTS public.migration_script
(
	id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "checksum" TEXT NOT NULL UNIQUE,
	"path" TEXT NOT NULL,
	executed_on TIMESTAMP NOT NULL DEFAULT NOW(),
	CONSTRAINT checksum_format CHECK (path ~ '^\S+$'),
    CONSTRAINT path_format CHECK (path ~ '^\S+(?:\s*\S+)*$')
);
COMMENT ON TABLE "public".migration_script IS 'Tracks migration scripts with their paths, checksums, and execution times.';

CREATE INDEX IF NOT EXISTS ix_migration_script_path ON "public".migration_script (LOWER(path));
COMMENT ON INDEX public.ix_migration_script_path IS 'Provides case-insensitive index for migration script paths.';


REVOKE ALL ON TABLE public.migration_script FROM PUBLIC;
GRANT SELECT ON TABLE public.migration_script TO PUBLIC;
GRANT INSERT, UPDATE, DELETE ON TABLE public.migration_script TO ${databaseOwner};


-- Create a function to execute migration scripts if they are new.

-- Function to execute SQL commands associated with migration scripts.
CREATE OR REPLACE FUNCTION public.execute_migration_script(p_data jsonb) RETURNS BIGINT
AS
$migration_script_execute$
DECLARE
    v_inserted_record_id BIGINT;  
    v_path text := NULLIF(TRIM(p_data->>'filePath'),'');
    v_command text := NULLIF(TRIM(p_data->>'command'),'');
    v_checksum text := NULLIF(TRIM(p_data->>'checksum'),'');
    v_migration_script public.migration_script;
BEGIN

    IF v_path IS NULL THEN
        RAISE EXCEPTION 'Required JSON property missing: "filePath"';
    END IF;

    IF v_command IS NULL THEN
        RAISE EXCEPTION 'Required JSON property missing: "command"';
    END IF;

    IF v_checksum IS NULL THEN
        RAISE EXCEPTION 'Required JSON property missing: "checksum"';
    END IF;

    SELECT ms.*
    INTO v_migration_script
    FROM public.migration_script AS ms
    WHERE ms."checksum" = v_checksum
    LIMIT 1
    FOR UPDATE;

    IF FOUND THEN
        IF v_path = (v_migration_script).path THEN
            RAISE NOTICE 'Script "%" already executed; operation skipped.', v_path;
        ELSE
            UPDATE public.migration_script
            SET path = v_path
            WHERE ms."checksum" = v_checksum;
            RAISE NOTICE 'Script "%" moved/renamed to "%"; operation skipped.', (v_migration_script).path, v_path;              
        END IF;
        RETURN -1;
    END IF;

    INSERT INTO public.migration_script("path", "checksum")
    VALUES (v_path, v_checksum)
    RETURNING id INTO v_inserted_record_id;

    EXECUTE v_command;

    RETURN v_inserted_record_id;
END;
$migration_script_execute$ LANGUAGE plpgsql;

-- Set function ownership for consistency.
ALTER FUNCTION public.execute_migration_script(jsonb) OWNER TO ${databaseName};

-- Add comments to describe the function's purpose and usage.

-- Function to execute a SQL statement if the provided migration script path is new.
COMMENT ON FUNCTION public.execute_migration_script(p_request jsonb) IS
'Executes or skips SQL commands based on migration script checksum and path changes in JSONB input. Requires "filePath", "command", and "checksum". Returns script ID or -1 if skipped.';


