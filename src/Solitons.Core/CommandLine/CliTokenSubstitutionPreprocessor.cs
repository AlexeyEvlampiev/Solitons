using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public delegate string CliTokenDecoder(string token);
internal interface ICliTokenEncoder
{
    string Encode(string text, out CliTokenDecoder decoder);
}

public sealed class CliTokenEncoder : ICliTokenEncoder
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly Dictionary<string, string> _substitutions;

    private CliTokenEncoder()
    {
        _substitutions = new Dictionary<string, string>();
    }

    string ICliTokenEncoder.Encode(string commandLine, out CliTokenDecoder decoder)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        decoder = (key) => dictionary.GetValueOrDefault(key, key);

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

    [DebuggerStepThrough]
    public static string Encode(string commandLine, out CliTokenDecoder decoder)
    {
        ICliTokenEncoder encoder = new CliTokenEncoder();
        return encoder.Encode(commandLine, out decoder);
    }

}