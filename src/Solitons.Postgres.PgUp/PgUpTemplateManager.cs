using Solitons.CommandLine;


namespace Solitons.Postgres.PgUp;

internal sealed class PgUpTemplateManager
{
    public static void Initialize(string projectDir, string template)
    {
        DirectoryInfo targetDir;
        try
        {
            targetDir = new DirectoryInfo(projectDir);
            if (targetDir.Exists == false)
            {
                CliExit.With($"'{targetDir.Name}' directory does not exist.");
            }
        }
        catch (Exception e)
        {
            CliExit.With($"'{projectDir}' is not a valid directory path.");
            throw;
        }

        if (targetDir.EnumerateFileSystemInfos().Any())
        {
            CliExit.With($"'{targetDir.Name}' directory is not empty..");
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
            CliExit.With($"The '{template}' template is not found.");
        }

        sourceDir.CopyContentsTo(targetDir, includeSubdirectories: true);
    }

    public static IEnumerable<Template> GetTemplateDirectories()
    {
        return Directory
            .EnumerateDirectories(".", "templates", SearchOption.AllDirectories)
            .Select(path => new DirectoryInfo(path))
            .GroupBy(root => root, root => root
                .GetDirectories("*", SearchOption.AllDirectories)
                .Where(di => di.Exists && di.EnumerateFiles("pgup.json", SearchOption.TopDirectoryOnly)
                    .Any()))
            .SelectMany(group =>
            {
                var root = group.Key;
                return group
                    .SelectMany(t => t)
                    .Select(di => new Template(
                        Path.GetRelativePath(root.FullName, di.FullName).ToLower(),
                        di));
            });
    }

    public sealed record Template(string Name, DirectoryInfo Root);
}