using System.Text.Json;
using Solitons.Collections;

namespace Solitons.Postgres.PgUp.Management;

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
        string? versionText = ThrowIf.NullOrWhiteSpace(jsonObject.GetProperty("version").GetString(), "pgup version is missing");
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
                    typeof(IProject).IsAssignableFrom(type) == false)
                {
                    return [];
                }
                return FluentArray.Create(type);
            })
            .Do((type, index) =>
            {
                if (index > 0)
                {
                    throw new InvalidOperationException("Multiple matches");
                }
            })
            .Select(t => new PgUpVersionAttribute(version, t))
            .Single();
    }

    public IProject Deserialize(string pgUpJson)
    {
        return (IProject)JsonSerializer.Deserialize(pgUpJson, _type)!;
    }

    public string Serialize(IProject project) => JsonSerializer.Serialize(project);
}