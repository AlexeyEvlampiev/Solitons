using System.Data.Common;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Solitons.Data;
using Solitons.Postgres.PgUp.Management.Models;

namespace Solitons.Postgres.PgUp.Management;

internal sealed class PgUpSerializer
{
    private static IDataContractSerializer _serializer;

    static PgUpSerializer()
    {
        _serializer = IDataContractSerializer.Build(config => config
            .AddAssemblyTypes(Assembly
                .GetEntryAssembly()));
    }

    public static IProject Deserialize(
        string pgUpJson, 
        Dictionary<string, string> parameters)
    {
        pgUpJson = PgUpScriptPreprocessor.Transform(pgUpJson, parameters);
        var project = _serializer
            .Deserialize<PgUpProjectJson>(pgUpJson, "application/json");
        for (int i = 0; i < 1000; ++i)
        {
            int resolvedParametersCount = 0;
            foreach (var key in project.Parameters.Keys)
            {
                var value = project.Parameters[key].Default;
                if (parameters.ContainsKey(key) ||
                    value is null ||
                    Regex.IsMatch(value, @"\${([^\s{}]+)}"))
                {
                    continue;
                }
                parameters[key] = value!;
                resolvedParametersCount++;
            }

            pgUpJson = project.ToString();
            if (resolvedParametersCount == 0)
            {
                break;
            }

            pgUpJson = PgUpScriptPreprocessor.Transform(pgUpJson, parameters);
            project = _serializer
                .Deserialize<PgUpProjectJson>(pgUpJson, "application/json");
        }

        


        pgUpJson = Regex.Replace(pgUpJson, @"\${([^\s{}]+)}", m =>
        {
            var key = m.Groups[1].Value;
            if (parameters.TryGetValue(key, out var value))
            {
                return value;
            }

            return m.Value;
        });

        foreach (var parameter in parameters)
        {
            project.SetDefaultParameterValue(parameter.Key, parameter.Value);
        }

        foreach (var parameter in parameters)
        {
            var placeholder = $"${{{parameter.Key}}}";
            pgUpJson = pgUpJson.Replace(placeholder, parameter.Value);
        }

        project = _serializer.Deserialize<PgUpProjectJson>(pgUpJson, "application/json");
        foreach (var parameter in parameters)
        {
            project.SetDefaultParameterValue(parameter.Key, parameter.Value);
        }

        pgUpJson = _serializer.Serialize(project).Content;


        project
            .Parameters
            .Keys
            .ToList()
            .Except(parameters.Keys)
            .ForEach(key =>
            {
                parameters[key] = project
                    .Parameters[key].Default;
            });

        while (parameters.Keys
               .Any(key => pgUpJson.Contains($"${{{key}}}", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var parameter in parameters)
            {
                var placeholder = $"${{{parameter.Key}}}";
                pgUpJson = pgUpJson.Replace(placeholder, parameter.Value, StringComparison.OrdinalIgnoreCase);
            }
        }
        

        var regex = new Regex($@"\${{(\S+?)}}");
        var unresolvedParametersCsv = regex
            .Matches(pgUpJson)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            .Join(", ");

        if (unresolvedParametersCsv.IsPrintable())
        {
            throw new InvalidOperationException(
                $"Unresolved parameters: {unresolvedParametersCsv}");
        }
        project = _serializer.Deserialize<PgUpProjectJson>(pgUpJson, "application/json");
        return project;
    }



}