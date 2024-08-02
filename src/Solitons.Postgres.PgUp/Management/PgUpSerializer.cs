using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Data;

namespace Solitons.Postgres.PgUp.Management;

internal sealed class PgUpSerializer
{
    private static readonly IDataContractSerializer Serializer;
    private static readonly Regex ParameterRefRegex = new Regex(@"\${([^\s{}]+)}");

    static PgUpSerializer()
    {
        Serializer = IDataContractSerializer.Build(config => config
            .AddAssemblyTypes(Assembly
                .GetEntryAssembly()!));
    }

    public static IProject Deserialize(
        string pgUpJson, 
        Dictionary<string, string> parameters)
    {
        var version = PgUpVersionAttribute.FromJson(pgUpJson);
        pgUpJson = SubstitudeParameters(pgUpJson, parameters);
        var project = version.Deserialize(pgUpJson);

        for (int i = 0;; ++i)
        {
            if (i > 1000)
            {
                throw new InvalidOperationException();
            }

            int resolvedParametersCount = 0;
            foreach (var key in project.ParameterNames)
            {
                if (parameters.ContainsKey(key) ||
                    project.HasDefaultParameterValue(key, out string value) == false ||
                    ParameterRefRegex.IsMatch(value))
                {
                    continue;
                }
     
                parameters[key] = value!;
                resolvedParametersCount++;
            }

            if (resolvedParametersCount == 0)
            {
                break;
            }

            pgUpJson = SubstitudeParameters(pgUpJson, parameters);
            project = version.Deserialize(pgUpJson);
        }

        foreach (var key in project.ParameterNames)
        {
            if (parameters.ContainsKey(key))
            {
                continue;
            }

            if (project.HasDefaultParameterValue(key, out var value))
            {
                parameters[key] = value;
                continue;
            }

            throw new InvalidOperationException("Parameter value is required");
        }

        return project;
    }

    private static string SubstitudeParameters(
        string input,
        IReadOnlyDictionary<string, string> parameters)
    {
        return ParameterRefRegex.Replace(input, m =>
        {
            var key = m.Groups[1].Value;
            if (parameters.TryGetValue(key, out var value))
            {
                return value;
            }

            return m.Value;
        });
    }

}