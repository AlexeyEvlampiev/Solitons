using System.Text.Json;
using Solitons.Collections;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PgUpVersionAttribute : Attribute
{
    private readonly Type _type;

    public PgUpVersionAttribute(string version)
    {
        Version = Version.Parse(version);
    }

    private PgUpVersionAttribute(Version version, Type type)
    {
        _type = type;
        Version = version;
    }

    public Version Version { get;  }

    public static PgUpVersionAttribute FromJson(string pgUpJson)
    {
        JsonElement jsonObject = JsonSerializer.Deserialize<JsonElement>(pgUpJson);
        string? versionText = jsonObject.GetProperty("version").GetString();
        if (string.IsNullOrWhiteSpace(versionText))
        {
            CliExit.With("PgUp version is missing. Ensure that the version json element is present in the pgup.json file.");
        }
        var version = Version.Parse(versionText!);
        return typeof(PgUpVersionAttribute)
            .Assembly
            .GetTypes()
            .SelectMany(type =>
            {
                var att = type
                    .GetCustomAttributes(true)
                    .OfType<PgUpVersionAttribute>()
                    .SingleOrDefault();
                if (att is null ||
                    att.Version != version ||
                    typeof(IPgUpProject).IsAssignableFrom(type) == false)
                {
                    return [];
                }
                return FluentArray.Create(type);
            })
            .Do((type, index) =>
            {
                if (index > 0)
                {
                    throw new InvalidOperationException($"Multiple {version} version matches found.");
                }
            })
            .Select(t => new PgUpVersionAttribute(version, t))
            .Single();
    }

    public IPgUpProject Deserialize(string pgUpJson)
    {
        try
        {
            return (IPgUpProject)JsonSerializer.Deserialize(pgUpJson, _type)!;
        }
        catch (JsonException e)
        {
            var message = e.Message.Replace(_type.ToString(), $"PgUp JSON v{Version}");
            CliExit.With($"Invalid pgup.json file. {message} (path: {e.Path}. line: {e.LineNumber})");
            throw;
        }
        catch (Exception e)
        {
            CliExit.With($"Failed to parse pgup.json file. {e.Message}");
            throw;
        }
    }

    public string Serialize(IPgUpProject project) => JsonSerializer.Serialize(project);
}