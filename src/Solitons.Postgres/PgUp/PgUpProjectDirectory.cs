using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

public sealed class PgUpProjectDirectory(string capture) :
    CliParameter<DirectoryInfo>(
        capture,
        "--project-directory|-dir",
        "File directory where to initialize the new pgup project.")
{
    public PgUpProjectDirectory() 
        : this(".")
    {
        
    }
}