using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.Postgres.PgUp.Core;

internal sealed class PgUpTemplateManager
{
    sealed record Resource(string EmbeddedResourceName, string FilePath);

    private readonly Assembly _assembly;
    private readonly Dictionary<string, Resource[]> _templates;

    public PgUpTemplateManager()
    {
        var prefix = $"{typeof(Program).Namespace}.Templates.";
        var directorySeparator = Path.DirectorySeparatorChar.ToString();
        _assembly = Assembly.GetExecutingAssembly();
        _templates = _assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .Convert(items =>
            {
                return items
                    .Where(i => i.EndsWith(".pgup.json"))
                    .Select(projectResource =>
                    {
                        var projectPath = projectResource.RemoveSuffix("pgup.json");
                        var resources = items
                            .Where(i => i.StartsWith(projectPath))
                            .Select(i => new Resource(i, i
                                .RemovePrefix(projectPath)
                                .Replace(new Regex(@"\.(?!(?:json|sql)$)"), directorySeparator)
                                .Replace("_", "-")))
                            .ToArray();
                        var templateName = projectPath
                            .Replace(prefix, String.Empty)
                            .Trim('.')
                            .Replace(".", directorySeparator);
                        return new
                        {
                            TemplateName = templateName,
                            Resources = resources
                        };
                    });
            })
            .ToDictionary(i => i.TemplateName, i => i.Resources);
    }
    public void Initialize(string projectDir, string template)
    {
        if (false == _templates.TryGetValue(template, out var resources))
        {
            throw new PgUpExitException($"'{template}' template not found."){ ExitCode = 4 };
        }

        
        

        Trace.WriteLine($"{typeof(PgUpTemplateManager)}.{nameof(Initialize)}");
        Trace.WriteLine($@"(\t""{projectDir}"", ""{template}"")");
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

        Trace.WriteLine($"Initializing '{template}' pgup template at '{targetDir.FullName}'");
        foreach (var resource in resources)
        {
            using var stream = _assembly.GetManifestResourceStream(resource.EmbeddedResourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"'{resource.EmbeddedResourceName}' resource not found");
            }

            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            var path = Path.Combine(targetDir.FullName, resource.FilePath);
            string directory = Path.GetDirectoryName(path)!;
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, content);
        }

    }

    public IEnumerable<string> GetTemplates()
    {
        return _templates.Keys;
    }
}