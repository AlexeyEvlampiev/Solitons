using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

internal sealed class PgUpDirectoryManager
{
    public static void Initialize(string projectDir, string template)
    {
        DirectoryInfo targetDir;
        try
        {
            targetDir = new DirectoryInfo(projectDir);
            if (targetDir.Exists == false)
            {
                throw new CliExitException($"'{targetDir.Name}' directory does not exist.");
            }
        }
        catch (Exception e)
        {
            throw new CliExitException($"'{projectDir}' is not a valid directory path.");
        }

        if (targetDir.EnumerateFileSystemInfos().Any())
        {
            throw new CliExitException($"'{targetDir.Name}' directory is not empty..");
        }

        var root = new DirectoryInfo("templates");
        var sourceDir = root
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Where(di =>
            {
                var relPath = Path.GetRelativePath(root.FullName, di.FullName);
                return
                    relPath.Equals(template, StringComparison.OrdinalIgnoreCase) &&
                    di.EnumerateFiles("pgup.json").Any();
            })
            .FirstOrDefault();
        if (sourceDir is null)
        {
            throw new CliExitException($"The '{template}' template is not found.");
        }

        sourceDir.CopyContentsTo(targetDir, includeSubdirectories: true);
    }
}