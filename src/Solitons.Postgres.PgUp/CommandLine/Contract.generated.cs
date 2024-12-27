using System.ComponentModel;
using Solitons.CommandLine.Reflection;


public interface IJazzy
{
    /// <summary>
    /// Sets up a new PgUp project structure with required files and folders for PostgreSQL deployment in the specified directory
    /// </summary>
    /// <param name="projectDir">Directory to populate with the pgup.json project file and deployment sql scripts.</param>
    /// <param name="template"></param>
	[CliRoute("init|initialize")]
    [CliArgument(nameof(projectDir), description: "")]
    [Description("Sets up a new PgUp project structure with required files and folders for PostgreSQL deployment in the specified directory")]
    void Initialize(
        string projectDir,
        [CliOption("--template", "Directory to populate with the pgup.json project file and deployment sql scripts.")] string template = "basic");
}
