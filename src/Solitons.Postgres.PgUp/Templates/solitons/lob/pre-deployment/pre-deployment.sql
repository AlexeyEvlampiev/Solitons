-- Create a schema to hold PostgreSQL extensions separately from business data.

RESET ROLE; -- The following command require the current session role


-- Schema for holding PostgreSQL extensions, keeping them separate from business data.
CREATE SCHEMA IF NOT EXISTS "extensions" AUTHORIZATION ${dbowner};
COMMENT ON SCHEMA "extensions" IS 'Schema for holding PostgreSQL extensions, keeping them separate from business data.';

-- Create the required extensions within the "extensions" schema.

-- Extension to support storing sets of key-value pairs.
CREATE EXTENSION IF NOT EXISTS hstore SCHEMA extensions CASCADE;

-- Extension to provide cryptographic functions.
CREATE EXTENSION IF NOT EXISTS pgcrypto SCHEMA extensions CASCADE;

-- Extension to support trigram-based text similarity measurement.
CREATE EXTENSION IF NOT EXISTS pg_trgm SCHEMA extensions CASCADE;

-- Ensure the existence of the database manager role and grant necessary privileges.

DO $$ 
BEGIN 
    -- Check if the database manager role exists, and create it if not.
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '${dbmanager}') THEN
        CREATE ROLE ${dbmanager} LOGIN PASSWORD '${dbmanagerpwd}';
	ELSE
	    -- If the role exists, update its password and connection limit.
		ALTER ROLE ${dbmanager} WITH 
			PASSWORD '${dbmanagerpwd}'
			CONNECTION LIMIT 5;
    END IF;
	
	-- Add a comment to describe the purpose of the database manager role.
	COMMENT ON ROLE ${dbmanager} IS '${dbname} - database manager role with login';
	
	-- Grant the database owner role to the database manager.
	GRANT ${dbowner} TO ${dbmanager};
END $$;






-- Create schemas to organize database objects effectively.
SET ROLE ${dbowner};

-- Schema for system-wide data such as migrations, logs, and configuration settings.
CREATE SCHEMA IF NOT EXISTS "system" AUTHORIZATION ${dbowner};
COMMENT ON SCHEMA "system" IS 'Schema containing system-wide data such as migrations, logs, and configuration settings.';

-- Schema for storing business data.
CREATE SCHEMA IF NOT EXISTS "data" AUTHORIZATION ${dbowner};
COMMENT ON SCHEMA "data" IS 'Schema containing business data.';

-- Schema for non-tabular contracts such as JSON RPCs.
CREATE SCHEMA IF NOT EXISTS "api" AUTHORIZATION ${dbowner};
COMMENT ON SCHEMA "api" IS 'Schema containing objects for non-tabular contracts such as JSON RPCs.';

-- Set schema ownership for consistency.
ALTER SCHEMA "extensions" OWNER TO ${dbowner};
ALTER SCHEMA "system" OWNER TO ${dbowner};
ALTER SCHEMA "data" OWNER TO ${dbowner};
ALTER SCHEMA "api" OWNER TO ${dbowner};

-- Set the search path to prioritize schema lookup.
ALTER DATABASE ${dbname} SET search_path TO "api", "data", "system", "extensions";

-- Create a table to track executed migration scripts.

-- Table to store migration script paths and execution timestamps.
CREATE TABLE IF NOT EXISTS system.migration_script
(
	id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	"path" TEXT NOT NULL UNIQUE,
	executed_on TIMESTAMP NOT NULL DEFAULT NOW(),
	CONSTRAINT path_format CHECK (path ~ '^\S+(?:\s*\S+)*$')
);
COMMENT ON TABLE "system".migration_script IS 'Stores the relative paths and execution timestamps of executed migration scripts.';

-- Create a unique index on the migration script path for efficiency.

-- Index to ensure uniqueness of migration script paths.
CREATE UNIQUE INDEX IF NOT EXISTS ux_migration_script_path ON "system".migration_script (LOWER(path));
COMMENT ON INDEX system.ux_migration_script_path IS 'Ensures unique file paths for migration scripts by comparing them in a case-insensitive manner.';


REVOKE ALL ON TABLE system.migration_script FROM PUBLIC;
GRANT SELECT ON TABLE system.migration_script TO PUBLIC;
GRANT INSERT, UPDATE, DELETE ON TABLE system.migration_script TO ${dbOwner};

-- Create a function to execute migration scripts if they are new.

-- Function to execute SQL commands associated with migration scripts.
CREATE OR REPLACE FUNCTION system.migration_script_execute(p_path text, p_sql text) RETURNS BIGINT
AS
$$
DECLARE
    inserted_id BIGINT;  -- Variable to hold the ID of the newly inserted or existing script
BEGIN
    -- Attempt to insert the new script path
    INSERT INTO system.migration_script("path")
    VALUES ($1)
    ON CONFLICT ("path") DO NOTHING
    RETURNING id INTO inserted_id;

    -- If a new path was inserted, execute the provided SQL and return the script ID
    IF inserted_id IS NOT NULL THEN
        EXECUTE $2;
        RETURN inserted_id;
    ELSE
        -- If the path already exists, return (-1) to indicate no execution occurred
        RETURN (-1);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Set function ownership for consistency.
ALTER FUNCTION system.migration_script_execute(text, text) OWNER TO ${dbname};

-- Add comments to describe the function's purpose and usage.

-- Function to execute a SQL statement if the provided migration script path is new.
COMMENT ON FUNCTION system.migration_script_execute("path" text, "sql" text) IS
'This function executes a SQL statement if the provided migration script path is new. 
Parameters:
- path: Text representation of the relative path to the migration script.
- sql: The SQL command to execute if the migration script is not found in the database.
Returns:
- The ID of the newly inserted migration script if it was not already present, after successfully executing the provided SQL command.
- (-1) if the path already exists, indicating that the SQL command was not executed.';


