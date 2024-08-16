using System.Text.Json;
using NJsonSchema.Generation;
using NJsonSchema.Validation;
using Solitons.Collections;
using Solitons.CommandLine;

namespace Solitons.Postgres.PgUp;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PgUpVersionAttribute : Attribute
{
    private readonly Type _type;

    public PgUpVersionAttribute(string version)
    {
        Verion = Version.Parse(version);
    }

    private PgUpVersionAttribute(Version version, Type type)
    {
        _type = type;
        Verion = version;
    }

    public Version Verion { get;  }

    public static PgUpVersionAttribute FromJson(string pgUpJson)
    {
        JsonElement jsonObject = JsonSerializer.Deserialize<JsonElement>(pgUpJson);
        string? versionText = jsonObject.GetProperty("version").GetString();
        if (string.IsNullOrWhiteSpace(versionText))
        {
            throw new CliExitException("PgUp version is missing. Ensure that the version json element is present in the pgup.json file.");
        }
        var version = Version.Parse(versionText);
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
                    att.Verion != version ||
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
            throw new CliExitException($"Invalid pgup.json file. {e.Message} Path: {e.Path}. Line: {e.LineNumber}");
        }
        catch (Exception e)
        {
            throw new CliExitException($"Failed to parse pgup.json file. {e.Message}");
        }
        
    }

    public string Serialize(IPgUpProject project) => JsonSerializer.Serialize(project);
}