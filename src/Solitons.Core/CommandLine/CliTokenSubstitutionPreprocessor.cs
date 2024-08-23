using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public sealed class CliTokenSubstitutionPreprocessor
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly Dictionary<string, string> _substitutions;

    private CliTokenSubstitutionPreprocessor(Dictionary<string, string> substitutions)
    {
        _substitutions = substitutions;
    }

    public static string SubstituteTokens(string commandLine, out CliTokenSubstitutionPreprocessor cliTokenSubstitutionPreprocessor)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        cliTokenSubstitutionPreprocessor = new CliTokenSubstitutionPreprocessor(dictionary);

        commandLine = Regex.Replace(
            commandLine,
            @"(?:""$variable""|$variable)".Replace("$variable", "%[^%]+%"),
            match =>
            {
                var key = Guid.NewGuid().ToString("N");
                var value = match.Value.Trim('"').Trim('%');
                value = Environment.GetEnvironmentVariable(value) ?? match.Value;
                dictionary[key] = value;
                return key;
            });

        commandLine = Regex.Replace(
            commandLine,
        @"""([^""]*)""",
        match =>
            {
                var group = match.Groups[1];
                Debug.Assert(group.Success);
                var key = Guid.NewGuid().ToString("N");
                dictionary[key] = group.Value;
                return key;
            });


        return commandLine;
    }

    public string GetSubstitution(string key) => _substitutions.GetValueOrDefault(key, key);

    public static IEnumerable<string> Parse(string commandLine)
    {
        commandLine = SubstituteTokens(commandLine.Trim(), out var preProcessor);
        foreach (var key in Regex.Split(commandLine, @"\s+"))
        {
            yield return preProcessor.GetSubstitution(key);
        }
    }
}