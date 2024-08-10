CREATE TABLE IF NOT EXISTS data.project
(
	"id" varchar(150) NOT NULL UNIQUE,
	"description" text NOT NULL DEFAULT(''),
	started_on TIMESTAMP WITH TIME ZONE,
	PRIMARY KEY(object_id)
) INHERITS(system.gc_object);


CREATE TABLE IF NOT EXISTS data.activity
(
	project_object_id uuid NOT NULL REFERENCES data.project(object_id),
	"id" varchar(150) NOT NULL UNIQUE,
	"description" text NOT NULL DEFAULT(''),
	duration interval NOT NULL,
	started_on TIMESTAMP WITH TIME ZONE,
	completed_on TIMESTAMP WITH TIME ZONE,
	effective_duration interval GENERATED ALWAYS AS (
		CASE
			WHEN completed_on IS NOT NULL THEN '0 days'::interval
			ELSE duration
		END
	) STORED,
	PRIMARY KEY(object_id),
	UNIQUE(project_object_id, object_id),
	CONSTRAINT non_negative_duration CHECK(duration >= '0 days'::interval),
	CONSTRAINT completedutc_ge_started_on CHECK(completed_on >= started_on)
) INHERITS(system.gc_object);



CREATE TABLE IF NOT EXISTS data.dependency
(
	project_object_id uuid NOT NULL,
	activity_object_id uuid NOT NULL,
	precondition_activity_object_id uuid NOT NULL CHECK(activity_object_id <> precondition_activity_object_id),
	created_on TIMESTAMP WITH TIME ZONE DEFAULT(now()),
	PRIMARY KEY(activity_object_id, precondition_activity_object_id),	
	FOREIGN KEY(project_object_id, activity_object_id) REFERENCES data.activity(project_object_id, object_id),
	FOREIGN KEY(project_object_id, precondition_activity_object_id) REFERENCES data.activity(project_object_id, object_id)
);



CREATE OR REPLACE FUNCTION data.resolve_dependency_chain(p_activity_object_id uuid)
RETURNS TABLE(
    activity_object_id uuid, 
	precondition_activity_object_id uuid, 
	activities_count int, 
	cumulative_thread_duration interval,
	path uuid[],
	is_critical_path boolean
) AS 
$$
	WITH RECURSIVE dependency_thread(activity_object_id, precondition_activity_object_id, activities_count, cumulative_thread_duration, path) AS
	(
		SELECT 
			a.object_id as activity_object_id,		
			d.precondition_activity_object_id,
			1 as activities_count,
			effective_duration AS duration,
			ARRAY[a.object_id] AS path
		FROM 
			data.activity as a
		LEFT JOIN 
			data.dependency as d 
				ON d.activity_object_id = a.object_id
		WHERE 
			a.object_id = p_activity_object_id
		AND a.deleted_on IS NULL
		UNION ALL
		SELECT 
			p.activity_object_id,
			d.precondition_activity_object_id,
			p.activities_count+1,
			p.cumulative_thread_duration + a.effective_duration,
			ARRAY[a.object_id]||p.path
		FROM dependency_thread as p
		INNER JOIN 
			data.activity AS a 
				ON a.object_id = p.precondition_activity_object_id
		LEFT JOIN 
			data.dependency as d 
				ON d.activity_object_id = a.object_id
	)
	SELECT 
		activity_object_id, 
		precondition_activity_object_id, 
		activities_count,
		cumulative_thread_duration,
		path,
		(CASE ROW_NUMBER() OVER(PARTITION BY activity_object_id ORDER BY cumulative_thread_duration DESC) 
			WHEN 1 THEN true
			ELSE false
	 	END) AS is_critical_path
	FROM dependency_thread
	LIMIT 1000;
$$ LANGUAGE sql;


CREATE OR REPLACE VIEW data.vw_dependency_space AS
SELECT 
	c_project.id,
	c_activity.id AS activity_id,
	c_precondition_activity.id AS precondition_activity_id,
	c_dependency.created_on,
	c_project.object_id AS project_object_id,
	c_activity.object_id AS activity_object_id,
	c_precondition_activity.object_id AS precondition_activity_object_id
FROM
	data.project AS c_project
INNER JOIN 
	data.activity AS c_activity 
		ON c_activity.project_object_id = c_project.object_id
		AND c_activity.deleted_on IS NULL
INNER JOIN 
	data.activity AS c_precondition_activity 
		ON c_precondition_activity.project_object_id = c_project.object_id
		AND c_precondition_activity.deleted_on IS NULL
		AND c_precondition_activity.object_id <> c_activity.object_id
LEFT JOIN
	data.dependency AS c_dependency
		ON c_dependency.project_object_id = c_project.object_id
		AND c_dependency.activity_object_id = c_activity.object_id
		AND c_dependency.precondition_activity_object_id = c_precondition_activity.object_id
WHERE
	NOT EXISTS
	(
		SELECT *
		FROM data.resolve_dependency_chain(c_precondition_activity.object_id) AS c_path
		WHERE c_path.precondition_activity_object_id = c_activity.object_id
	);	


CREATE OR REPLACE FUNCTION data.check_circular_dependency()
RETURNS TRIGGER AS $$
DECLARE
    v_activity_object_id uuid;
	v_precondition_activity_object_id uuid;
BEGIN
    v_activity_object_id := NEW.activity_object_id;
	v_precondition_activity_object_id := NEW.precondition_activity_object_id;
	
	IF NOT EXISTS (
		SELECT * 
		FROM data.vw_dependency_space
		WHERE 
			activity_object_id = v_activity_object_id
		AND precondition_activity_object_id = v_precondition_activity_object_id) THEN
		RAISE EXCEPTION 'Circular dependency detected';
	END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;



CREATE TRIGGER trg_check_circular_dependency
BEFORE INSERT ON data.dependency
FOR EACH ROW EXECUTE FUNCTION data.check_circular_dependency();

CREATE OR REPLACE FUNCTION data.project_metrics(p_project_object_id uuid) RETURNS
TABLE
(
	completed_on timestamp,
	progress_percentage float,
	critical_path_duration interval,
	activities_count int,
	completed_activities_count int,
	average_activity_duration interval,
	shortest_activity_duration interval,
	longest_activity_duration interval
)
AS
$$
	SELECT 
		(CASE 
			WHEN c_activity_stats.completed_count = c_activity_stats.count THEN c_activity_stats.latest_completed_on
			ELSE NULL
		 END) AS completed_on,
		ROUND(100.0 * c_activity_stats.completed_count / GREATEST(c_activity_stats.count, 1), 2) AS progress_percentage,
		c_critical_path.duration AS critical_path_duration,
		c_activity_stats.count AS activities_count,
		c_activity_stats.completed_count AS completed_activities_count,
		c_activity_stats.average_duration AS average_activity_duration,
		c_activity_stats.shortest_duration AS shortest_activity_duration,
		c_activity_stats.longest_duration AS longest_activity_duration
	FROM 
		data.project AS c_project
	CROSS JOIN LATERAL
	(
		SELECT MAX("chain".cumulative_thread_duration)
		FROM 
			data.activity AS c_activity,
			data.resolve_dependency_chain(c_activity.object_id) AS "chain"
		WHERE
			c_activity.project_object_id = c_project.object_id
	) AS c_critical_path(duration)
	CROSS JOIN LATERAL
	(
		SELECT 
			COUNT(*) AS "count",
			COUNT(*) FILTER(WHERE c_activity.completed_on IS NOT NULL) AS completed_count,
			COUNT(*) FILTER(WHERE c_activity.completed_on IS NULL) AS pending_count,
			MAX(c_activity.duration) longest_duration,
			MIN(c_activity.duration) shortest_duration,
			MAX(c_activity.completed_on) AS latest_completed_on,
			AVG(c_activity.duration) average_duration
		FROM 
			data.activity AS c_activity
		WHERE
			c_activity.project_object_id = c_project.object_id
		AND c_activity.deleted_on IS NULL
	) AS c_activity_stats
	WHERE
		c_project.object_id = p_project_object_id
	AND c_project.deleted_on IS NULL;
$$ LANGUAGE SQL;



CREATE OR REPLACE FUNCTION data.critical_path(p_project_object_id uuid) RETURNS
TABLE
(	
	activity_id varchar(150),
    sequence_id int,
	duration interval,
	effective_duration interval,
	cumulative_duration interval,
	description text,
	activity_object_id uuid
)
AS
$$
	SELECT 
		c_activity.id,
		ROW_NUMBER() OVER (
			PARTITION BY critical_path.activity_object_id 
			ORDER BY array_position(critical_path.path, c_activity.object_id)
		) as sequence_id,
		c_activity.duration,
		c_activity.effective_duration,
		SUM(c_activity.effective_duration) OVER (
			PARTITION BY critical_path.activity_object_id 
			ORDER BY array_position(critical_path.path, c_activity.object_id)
		) as cumulative_duration,
		c_activity.description,
		c_activity.object_id AS activity_object_id
	FROM 
		data.project AS c_project
	LEFT JOIN LATERAL
	(
		SELECT c_path.*
		FROM
			data.activity AS c_activity,
			data.resolve_dependency_chain(c_activity.object_id) AS c_path
		WHERE
			c_activity.project_object_id = c_project.object_id
		AND c_activity.deleted_on IS NULL
		ORDER BY c_path.cumulative_thread_duration DESC
		LIMIT 1
	) AS critical_path ON true
	INNER JOIN 
		data.activity AS c_activity 
			ON c_activity.object_id = ANY(critical_path.path)
			AND c_activity.deleted_on IS NULL
	WHERE 
		c_project.object_id = p_project_object_id
	AND	c_project.deleted_on IS NULL;
$$ LANGUAGE SQL;



CREATE OR REPLACE FUNCTION data.get_dependencies(p_project_object_id uuid) RETURNS
TABLE
(
	activity_id varchar(150),
	precondition_activity_id varchar(150),
	is_critical bool,
	activity_object_id uuid,
	precondition_activity_object_id uuid
) 
AS
$$
	SELECT
		c_activity.id AS activity_id,
		c_precondition_activity.id AS precondition_activity_id,
		(critical_path_source.activity_object_id IS NOT NULL) AS is_critical,
		c_dependency.activity_object_id,
		c_dependency.precondition_activity_object_id
	FROM
		data.project AS c_project	
	INNER JOIN
		data.dependency AS c_dependency
			ON c_dependency.project_object_id = p_project_object_id
	INNER JOIN
		data.activity AS c_activity
			ON c_activity.object_id = c_dependency.activity_object_id
			AND c_activity.deleted_on IS NULL
	INNER JOIN
		data.activity AS c_precondition_activity
			ON c_precondition_activity.object_id = c_dependency.precondition_activity_object_id
			AND c_precondition_activity.deleted_on IS NULL			
	LEFT JOIN 
		data.critical_path(p_project_object_id) AS critical_path_target
			ON critical_path_target.activity_object_id = c_dependency.activity_object_id
	LEFT JOIN 
		data.critical_path(p_project_object_id) AS critical_path_source
			ON critical_path_target.activity_object_id IS NOT NULL
			AND critical_path_source.activity_object_id = c_dependency.precondition_activity_object_id
			AND 1 = (critical_path_target.sequence_id  - critical_path_source.sequence_id)
	WHERE 
		c_project.object_id = p_project_object_id;
$$ LANGUAGE SQL;


CREATE OR REPLACE VIEW data.vw_activity AS
SELECT 
	c_project.id AS project_id,
	ROW_NUMBER() OVER(PARTITION BY c_project.object_id ORDER BY c_path.cumulative_thread_duration ASC) AS logical_id,
	c_activity.id AS "id",
	c_activity.duration,
	c_activity.effective_duration,
	c_path.cumulative_thread_duration AS commulative_estimate,
	(c_critical_path.activity_object_id IS NOT NULL) AS is_critical,
	c_activity.started_on,
	c_activity.completed_on,	
	c_activity.description,
	c_activity.object_id,
	c_activity.project_object_id
FROM 
	data.project AS c_project
INNER JOIN
	data.activity AS c_activity
		ON c_activity.project_object_id = c_project.object_id
		AND c_activity.deleted_on IS NULL
INNER JOIN 
	data.resolve_dependency_chain(c_activity.object_id) AS c_path
		ON c_path.is_critical_path		
LEFT JOIN LATERAL
	data.critical_path(c_project.object_id) AS c_critical_path
		ON c_critical_path.activity_object_id = c_activity.object_id
WHERE
	c_project.deleted_on IS NULL;



CREATE OR REPLACE VIEW data.vw_critical_path AS
SELECT 
	c_project.id AS project_id,
	c_critical_path.*,
	c_project.object_id AS project_object_id
FROM 
	data.project AS c_project,
	data.critical_path(c_project.object_id) AS c_critical_path
WHERE	
    c_project.deleted_on IS NULL;



CREATE OR REPLACE FUNCTION data.graphml_build_project(p_project_object_id uuid)
RETURNS xml AS $$
DECLARE
    nodes xml;
    edges xml;
BEGIN
    -- Nodes from vw_activity
    nodes := (
        SELECT XMLAGG(
            XMLELEMENT(
                NAME "node",
                XMLATTRIBUTES(
                    c_activity.id AS "id"
                ),
                XMLELEMENT(
                    NAME "data",
                    XMLATTRIBUTES('name' AS "key"),
                    FORMAT('%s - %s', c_activity.logical_id, c_activity.id)
                ),
				XMLELEMENT(
                    NAME "data",
                    XMLATTRIBUTES('IsCritical' AS "key"),
                    c_activity.is_critical
                ),
                XMLELEMENT(
                    NAME "data",
                    XMLATTRIBUTES('description' AS "key"),
                    c_activity.description
                )
            )
        )
        FROM data.vw_activity AS c_activity
        WHERE c_activity.project_object_id = p_project_object_id		
    );


    edges := (
        SELECT XMLAGG(
            XMLELEMENT(
                NAME "edge",
                XMLATTRIBUTES(
                    c_dependency.activity_id AS "target",
                    c_dependency.precondition_activity_id AS "source"
                ),
				XMLELEMENT(
                    NAME "data",
                    XMLATTRIBUTES('IsCritical' AS "key"),
                    c_dependency.is_critical
                )
            )
        )
        FROM data.get_dependencies(p_project_object_id) c_dependency
    );

    -- Construct the GraphML
    RETURN XMLELEMENT(
        NAME "graphml",
        XMLATTRIBUTES(
            'http://graphml.graphdrawing.org/xmlns' AS "xmlns",
            'http://www.w3.org/2001/XMLSchema-instance' AS "xmlns:xsi",
            'http://graphml.graphdrawing.org/xmlns http://graphml.graphdrawing.org/xmlns/1.0/graphml.xsd' AS "xsi:schemaLocation"
        ),
        XMLELEMENT(
            NAME "key",
            XMLATTRIBUTES('name' AS "id", 'node' AS "for", 'name' AS "attr.name", 'string' AS "attr.type")
        ),
        XMLELEMENT(
            NAME "key",
            XMLATTRIBUTES('description' AS "id", 'node' AS "for", 'description' AS "attr.name", 'string' AS "attr.type")
        ),
		XMLELEMENT(
            NAME "key",
            XMLATTRIBUTES('IsCritical' AS "id", 'node' AS "for", 'IsCritical' AS "attr.name", 'boolean' AS "attr.type")
        ),
		XMLELEMENT(
            NAME "key",
            XMLATTRIBUTES('IsCritical' AS "id", 'edge' AS "for", 'IsCritical' AS "attr.name", 'boolean' AS "attr.type")
        ),
        XMLELEMENT(
            NAME "graph",
            XMLATTRIBUTES('G' AS "id", 'directed' AS "edgedefault"),
            nodes,
            edges
        )
    );
END;
$$ LANGUAGE plpgsql;



CREATE OR REPLACE VIEW data.vw_project AS
SELECT 
	c_project.id,
	c_project.started_on,
	c_metrics.progress_percentage,
	c_metrics.critical_path_duration,
	c_metrics.activities_count,
	c_metrics.completed_activities_count,
	c_metrics.average_activity_duration,
	c_metrics.shortest_activity_duration,
	c_metrics.longest_activity_duration,	
	c_project.description,
	c_project.created_on,
	data.graphml_build_project(c_project.object_id) AS graphml,
	c_project.object_id
FROM 
	data.project AS c_project,
	data.project_metrics(c_project.object_id) AS c_metrics
WHERE
	c_project.deleted_on IS NULL;