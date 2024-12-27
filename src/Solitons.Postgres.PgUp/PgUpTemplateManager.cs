using System.Diagnostics;

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
                throw new PgUpExitException($"'{targetDir.Name}' directory does not exist.");
            }
        }
        catch (Exception e)
        {
            throw new PgUpExitException($"'{projectDir}' is not a valid directory path.");
        }

        if (targetDir.EnumerateFileSystemInfos().Any())
        {
            throw new PgUpExitException($"'{targetDir.Name}' directory is not empty..");
        }

        var root = new DirectoryInfo("Templates");
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
            throw new PgUpExitException($"The '{template}' template is not found.");
        }


        Console.WriteLine(@"=======================================================================");

        foreach (var xxx in sourceDir.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            Console.WriteLine(xxx.FullName);
        }

        sourceDir.CopyContentsTo(targetDir, includeSubdirectories: true);
    }

    public static IEnumerable<Template> GetTemplateDirectories()
    {
        var dir = new DirectoryInfo(Path.Combine(".", "Templates"));
        Debug.Assert(dir.Exists);
        return dir
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Where(subDir => subDir
                .GetFiles("*", SearchOption.TopDirectoryOnly)
                .Any(f => f.Name.Equals("pgup.json")))
            .Select(templateDir => new Template(templateDir.Name.ToLower(), templateDir));
    }

    public sealed record Template(string Name, DirectoryInfo Root);
}