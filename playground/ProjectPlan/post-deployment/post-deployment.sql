
DELETE FROM data.dependency WHERE project_object_id = '00146fbc-0ffc-471e-a6d6-d4b1b671c4d5';
DELETE FROM data.activity WHERE project_object_id = '00146fbc-0ffc-471e-a6d6-d4b1b671c4d5';
DELETE FROM data.project WHERE object_id = '00146fbc-0ffc-471e-a6d6-d4b1b671c4d5';
DO 
$init$
DECLARE 
	spacex_oid uuid := '00146fbc-0ffc-471e-a6d6-d4b1b671c4d5';
BEGIN

	INSERT INTO data.project(object_id, "id", "description") VALUES
	(spacex_oid, 'PM-WebApp', 'Description goes here');
	
	INSERT INTO data.activity(project_object_id, "id", "description", duration)
	SELECT spacex_oid, "id", "description", duration
	FROM 
	(
		VALUES
		('final-approval', $$The final phase in the project lifecycle, where the EcoVadis evaluates the overall quality and readiness of the Carbon Estimator (CE) application for public showcasing. This activity involves a comprehensive review to assess whether the application meets EV's high standards of quality, performance, and user experience. The outcome of this process determines EV's confidence in demonstrating the application at the Sustain conference, making it a decisive factor in the app's launch and public reception. The focus is on ensuring that the CE application not only functions flawlessly but also aligns with EV's values and expectations, particularly in the context of a significant industry event.$$, '5 days'::interval),
		('stress-test-setup', $$ involves creating and configuring a testing environment specifically designed to evaluate the performance and resilience of the Carbon Estimator (CE) application under heavy load conditions. This setup includes implementing tools and scripts capable of simulating a high volume of concurrent users, typically aiming to mimic the behavior of 1,000 users simultaneously interacting with the application. The primary objective of this setup is to rigorously test and identify any potential scalability issues, ensuring that the CE application can handle peak usage scenarios without degradation in performance, stability, or user experience.$$, '20 days'::interval),
		('stress-test-approval', $$Designed to rigorously evaluate the Carbon Estimator (CE) application's performance under extreme conditions. Specifically, this test simulates a scenario where 1,000 users are operating the application simultaneously, aiming to ensure that the app maintains its functionality, responsiveness, and stability under high load. The primary objective is to identify and address potential bottlenecks or performance issues, thereby securing approval based on the app's capability to handle heavy user traffic without compromising on performance or user experience.$$, '15 days'::interval),
		
		('view-completed-assessments', 'Use Case', '5 days'::interval),
		('view-interactive-dashboards', 'Use Case', '20 days'::interval),
		('view-company-estimates', 'Use Case', '5 days'::interval),
		('view-year-completed-assessments', 'Use Case', '5 days'::interval),
		('view-emissions-by-scope-or-category', 'Use Case', '5 days'::interval),
		('view-industry-average-comparison', 'Use Case', '10 days'::interval),
		('view-industry-average-based-parts-of-estimates', 'Use Case', '10 days'::interval),
		('compare-year-estimates', 'Use Case', '5 days'::interval),
		('export-results-in-excel', 'Use Case', '15 days'::interval),
		('export-results-in-pdf', 'Use Case', '20 days'::interval),
		
		('ux-redesign', $$Focuses on overhauling the user experience (UX) of the Carbon Estimator (CE) application to align with the EcoVadis design standards and aesthetic. This involves a comprehensive redesign of the application's user interface, ensuring consistency in visual elements, navigation, and overall user interaction. The goal is to achieve a seamless integration of the CE application within the EV ecosystem, offering users a harmonized and intuitive experience that resonates with the established EV brand identity.$$, '10 days'::interval),
		
		('acn-mirror-dev-environment', 'Involves the creation of a Mirror Environment within a private Azure subscription, mirroring the EcoVadis’s setup. This includes configuring Azure DevOps for streamlined continuous integration and deployment processes, establishing a Git repository for source code version control, and setting up an Azure Kubernetes Service (AKS) cluster. The cluster will host a simulated version of the EV platform, represented by a dummy web application that directs to the CE estimator, and the setup also comprises an Azure Container Registry for Docker image management, along with an Identity Server instance to replicate the SSO capabilities found in the EV environment.', '10 days'::interval),
		('ev-dev-environment', $$Entails the strategic transfer of the CE estimator application and its associated components from the Mirror Environment to the EcoVadis production infrastructure. This process involves replicating the application's setup, configurations, and integrations, including Azure DevOps pipelines, the AKS cluster, and Identity Server settings, ensuring seamless integration with the EV's existing systems. Careful coordination, thorough testing, and validation are key aspects of this migration to ensure compatibility and functionality within the EV's ecosystem.$$, '10 days'::interval),
		('azure-carbon-estimator', 'Involves setting up an automated build and deployment pipeline within Azure for the Carbon Estimator application. This process transitions the application from AWS to Azure, leveraging Azure DevOps for continuous integration and delivery. The goal is to achieve a fully operational CE application in the Azure environment, marked by efficiency and automation in its deployment and management.', '10 days'::interval),
		('release-candidate', 'Centered on compiling all the Minimum Viable Product (MVP) features of the Carbon Estimator (CE) application and deploying them to a staging environment of the EcoVadis. This stage serves as a crucial pre-release phase, where the integrated functionalities are thoroughly tested and validated in a controlled setting that mirrors the production environment. The primary objective is to ensure that the CE application performs reliably and meets all the defined requirements before its final release.', '3 days'::interval)
	) AS activities("id", "description", duration);
	
	INSERT INTO data.dependency(project_object_id, activity_object_id, precondition_activity_object_id)
	SELECT spacex_oid, target.object_id, precondition.object_id
	FROM data.activity AS target
	CROSS JOIN data.activity AS precondition
	WHERE 
		(target.project_object_id, precondition.project_object_id) = (spacex_oid,spacex_oid)
	AND (target.id, precondition.id) IN 
	(
		
		('azure-carbon-estimator', 'acn-mirror-dev-environment'),		
		('ev-dev-environment', 'acn-mirror-dev-environment'),
		('ev-dev-environment', 'azure-carbon-estimator'),
		
		('view-completed-assessments', 'azure-carbon-estimator'),
		('view-interactive-dashboards', 'azure-carbon-estimator'),
		('view-company-estimates', 'azure-carbon-estimator'),
		('view-year-completed-assessments', 'azure-carbon-estimator'),
		('view-emissions-by-scope-or-category', 'azure-carbon-estimator'),
		('view-industry-average-comparison', 'azure-carbon-estimator'),
		('view-industry-average-based-parts-of-estimates', 'azure-carbon-estimator'),
		('compare-year-estimates', 'azure-carbon-estimator'),
		('export-results-in-excel', 'azure-carbon-estimator'),
		('export-results-in-pdf', 'azure-carbon-estimator'),
		
		
		
		('release-candidate', 'view-completed-assessments'),
		('release-candidate', 'view-interactive-dashboards'),
		('release-candidate', 'view-company-estimates'),
		('release-candidate', 'view-year-completed-assessments'),
		('release-candidate', 'view-emissions-by-scope-or-category'),
		('release-candidate', 'view-industry-average-comparison'),
		('release-candidate', 'view-industry-average-based-parts-of-estimates'),
		('release-candidate', 'compare-year-estimates'),
		('release-candidate', 'export-results-in-excel'),
		('release-candidate', 'export-results-in-pdf'),
		('release-candidate', 'ux-redesign'),
		
		('release-candidate', 'ev-dev-environment'),
		
		
		('final-approval', 'stress-test-approval'),
		('stress-test-approval', 'release-candidate'),
		('stress-test-approval', 'stress-test-setup')
	);
	
END;
$init$;





--select * from data.vw_project where id = 'PM-WebApp';


SELECT 
	logical_id as serial,
	id as code,
	duration,
	(
		SELECT ARRAY_AGG(c_precondition.id)
		FROM UNNEST(preconditions) AS c_precondition		
	) AS preconditions,
	commulative_estimate,
	is_critical,
	description
FROM 
	data.vw_activity 
WHERE 
	project_id = 'PM-WebApp';


