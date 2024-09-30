using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Solitons.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal delegate string CliTokenDecoder(string token);
internal interface ICliTokenEncoder
{
    string Encode(string text, out CliTokenDecoder decoder);
}

internal sealed class CliTokenEncoder : ICliTokenEncoder
{
    private readonly Regex _keyValueIndexerOptionRegex;
    private readonly Regex _keyValueAccessorOptionRegex;
    private readonly Regex _programPathRegex;
    private readonly Regex _envVariableReferenceRegex;
    private readonly Regex _quotedTextRegex;


    [DebuggerStepThrough]
    private CliTokenEncoder()
    {
        _keyValueIndexerOptionRegex = new Regex(
            @"(?<option>$option) \s* \[ \s* (?<key>$key) \s* \]"
                .Replace("$option", @"\-{1,}\w[^\s\[]*")
                .Replace("$key", @"[^\[\]\s]+")
                .Convert(RegexUtils.RemoveWhitespace), RegexOptions.Compiled);
        _keyValueAccessorOptionRegex = new Regex(@"
            (?=\-)
            (?<=^|\s)
            (?<option>[^\s\.]+) \s* \. \s* 
            (?<key>[^\-\s]\S*)"
            .Convert(RegexUtils.RemoveWhitespace), RegexOptions.Compiled);
        _programPathRegex = new Regex(@"^\s*\S+", RegexOptions.Compiled);

        _envVariableReferenceRegex = new Regex(@"(?:""$variable""|$variable)".Replace("$variable", "%[^%]+%"), RegexOptions.Compiled);

        _quotedTextRegex = new Regex(@"""[^""]*""", RegexOptions.Compiled);
    }


    [DebuggerStepThrough]
    public static string Encode(string commandLine, out CliTokenDecoder decoder)
    {
        ICliTokenEncoder encoder = new CliTokenEncoder();
        return encoder.Encode(commandLine, out decoder);
    }


    string ICliTokenEncoder.Encode(string commandLine, out CliTokenDecoder decoder)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);

        string Decode(string input)
        {
            var comparer = StringComparer.Ordinal;

            for (int i = 0; i < 1000; ++i)
            {
                var result = input;
                foreach (var pair in dictionary)
                {
                    result = result.Replace(pair.Key, pair.Value);
                }

                if (comparer.Equals(result, input))
                {
                    return result;
                }

                input = result;
            }

            return input;
        }

        decoder = Decode;

        commandLine = _envVariableReferenceRegex.Replace(
            commandLine,
            match =>
            {
                var key = GenerateKey();
                var value = match.Value.Trim('"').Trim('%');
                value = Environment.GetEnvironmentVariable(value);
                if (value.IsNullOrWhiteSpace())
                {
                    return match.Value;
                }
                dictionary[key] = value!;
                return key;
            });

        commandLine = _quotedTextRegex.Replace(
            commandLine,
            match =>
            { ;
                var key = GenerateKey();
                dictionary[key] = match.Value.Trim('"');
                return key;
            });

        commandLine = _keyValueIndexerOptionRegex.Replace(commandLine, m => m.Result("${option}.${key}"));
        commandLine = _keyValueAccessorOptionRegex.Replace(commandLine, m => m.Result("${option}.${key}"));
        commandLine = _programPathRegex.Replace(commandLine, m =>
        {
            var path = m.Value.Trim();
            path = Decode(path);
            try
            {
                return Path
                    .GetFileName(path)
                    .DefaultIfNullOrWhiteSpace(path);
            }
            catch (Exception e)
            {
                Debug.Fail(e.Message);
                return path;
            }
            
        });
        return commandLine;
    }



    private static string GenerateKey() => Guid.NewGuid().ToString("N");

}