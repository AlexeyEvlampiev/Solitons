CREATE TABLE IF NOT EXISTS data.project
(
	"id" varchar(150) NOT NULL UNIQUE,
	"description" text NOT NULL DEFAULT(''),
	started_on TIMESTAMP WITH TIME ZONE,
	PRIMARY KEY(object_id)
) INHERITS(data.gc_object);