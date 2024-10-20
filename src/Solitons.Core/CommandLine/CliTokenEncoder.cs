using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal delegate string CliTokenDecoder(string token);


internal sealed class CliTokenEncoder
{
    delegate string Transformer(string commandLine, UniqueTokenGenerator generator);
    private readonly Dictionary<string, string> _tokens = new(StringComparer.Ordinal);
    private readonly Dictionary<object, Regex> _regexCache = new();
    private readonly Transformer[] _transformers;

    class UniqueTokenGenerator
    {
        private const string Prefix = "@val";
        private int _index = 0;
        private readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase);

        public UniqueTokenGenerator(string commandLine)
        {
            var regex = new Regex(@$"\b{Prefix}\d+\b");
            foreach (Match match in regex.Matches(commandLine))
            {
                _reserved.Add(match.Value);
            }
        }

        public string Next()
        {
            for (int i = 0; i < _reserved.Count; ++i)
            {
                var next = $"{Prefix}{_index++:00}";
                if (_reserved.Contains(next))
                {
                    continue;
                }

                return next;
            }

            throw new InvalidOperationException();
        }
    }


    [DebuggerStepThrough]
    private CliTokenEncoder()
    {
        _transformers =
        [
            TrimProgramPath,
            SubstituteEnvVariables,
            SubstituteQutatedText,
            SubstituteKeyValuePairs
        ];
    }


    [DebuggerStepThrough]
    public static string Encode(string commandLine, out CliTokenDecoder decoder)
    {
        var encoder = new CliTokenEncoder();
        decoder = encoder.Decode;
        return encoder.Encode(commandLine);
    }




    string Encode(string commandLine)
    {
        var generator = new UniqueTokenGenerator(commandLine);
        foreach (var transformer in _transformers)
        {
            commandLine = transformer.Invoke(commandLine, generator);
        }
        return commandLine;
    }

    string Decode(string token)
    {
        for (int i = 0; 
             i < _tokens.Count && _tokens.TryGetValue(token, out var decoded); 
             i++)
        {
            token = decoded;
        }

        return token;
    }

    string TrimProgramPath(string commandLine, UniqueTokenGenerator generator)
    {
        return _regexCache
            .GetOrAdd(nameof(TrimProgramPath), () =>
                @"^(?:""[^""]+""?|\S+)"
                    .Convert(pattern => new Regex(pattern)))
            .Replace(commandLine, match =>
            {
                var path = match.Value.Trim('"');
                try
                {
                    var file = Path
                        .GetFileName(path)
                        .DefaultIfNullOrWhiteSpace(path);
                    if (Regex.IsMatch(file, @"\s+"))
                    {
                        var token = generator.Next();
                        _tokens[token] = file;
                        return token;
                    }

                    _tokens[file] = path;
                    return file;
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message);
                    return path;
                }
            });
    }

    string SubstituteQutatedText(string commandLine, UniqueTokenGenerator generator)
    {
        return _regexCache
            .GetOrAdd(nameof(SubstituteQutatedText), () =>
                @"""[^""]*"""
                    .Convert(pattern => new Regex(pattern)))
            .Replace(commandLine, match =>
            {
                var token = generator.Next();
                var value = match.Value.Trim('"');
                _tokens[token] = value!;
                return token;
            });
    }

    string SubstituteEnvVariables(string commandLine, UniqueTokenGenerator generator)
    {
        return _regexCache
            .GetOrAdd(nameof(SubstituteEnvVariables), () =>
                @"(?:""$variable""|$variable)"
                    .Replace("$variable", "%[^%]+%")
                    .Convert(pattern => new Regex(pattern)))
            .Replace(commandLine, match =>
            {
                var token = generator.Next();
                var value = match.Value.Trim('%');
                var variableValue = Environment.GetEnvironmentVariable(value);
                if (variableValue.IsPrintable())
                {
                    value = variableValue;
                }
                
                _tokens[token] = value!;
                return token;
            });
    }
    

    string SubstituteKeyValuePairs(string commandLine, UniqueTokenGenerator generator)
    {
        return _regexCache
            .GetOrAdd(nameof(SubstituteKeyValuePairs), () =>
            @"(?<=\s)$option$key_value_pair"
                .Replace("$option", @"(?<option>\-{1,}[^\s\[\]\.]+)")
                .Replace("$key_value_pair", @"(?:$dot_notation|$accessor_notation)")
                .Replace("$dot_notation", @"(?:\s*\.\s*$key(?:\s+$value)?)")
                .Replace("$accessor_notation", @"(?:\s*\[\s*$key\s*\](?:\s+$value)?)")
                .Replace("$key", @"(?<key>\S+)")
                .Replace("$value", @"(?<value>[^\-\s]\S+)")
                .Convert(pattern => new Regex(pattern)))
            .Replace(commandLine, match =>
            {
                var token = generator.Next();
                _tokens[token] = match.Result("${key} ${value}").Trim();
                return match.Result("${option} {token}");
            });
    }

    private static string GenerateKey() => Guid.NewGuid().ToString("N");

}