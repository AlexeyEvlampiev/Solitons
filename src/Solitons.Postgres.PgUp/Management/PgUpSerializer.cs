using System.Reflection;
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
        foreach (var parameter in parameters)
        {
            var placeholder = $"${{{parameter.Key}}}";
            pgUpJson = pgUpJson.Replace(placeholder, parameter.Value);
        }

        var project = _serializer.Deserialize<PgUpProjectJson>(pgUpJson, "application/json");
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

        foreach (var parameter in parameters)
        {
            var placeholder = $"${{{parameter.Key}}}";
            pgUpJson = pgUpJson.Replace(placeholder, parameter.Value);
        }

        var regex = new Regex($@"\${{(\S+?)}}");
        var unresolvedParametersCsv = regex
            .Matches(pgUpJson)
            .Select(m => m.Groups[1].Value)
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