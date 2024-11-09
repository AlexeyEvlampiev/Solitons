using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Text.RegularExpressions;

namespace Solitons.CommandLine;

public sealed class ABCommandLine
{
    delegate string Transformer(string commandLine, State state);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Dictionary<string, string> _encodings = new(StringComparer.Ordinal);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ImmutableArray<object> _options;

    [DebuggerStepThrough]
    public static ABCommandLine Parse(string commandLine) => new(commandLine);

    private ABCommandLine(string commandLine)
    {
        CommandLine = commandLine;
        var state = new State(commandLine, _encodings);

        var transformers = new Transformer[]
        {
            EncodeCommandName,
            EncodeEnvVariables,
            EncodeQuotedText,
            FormatMapOptions,
            StripOptions
        };

        foreach (var transformer in transformers)
        {
            commandLine = transformer.Invoke(commandLine, state);
        }

        CanonicalForm = commandLine;
        ExecutableName = ThrowIf.NullOrWhiteSpace(state.ProgramName).Trim();
        _options = state.Options;
    }


    public int OptionCount => _options.Length;


    public string ExecutableName { get; }

    public string CommandLine { get; }

    public string CanonicalForm { get; }



    public bool IsFlagOption(int index, out string optionName)
    {
        if (TryGetOption<KeyValuePair<string, Unit>>(index, out var pair))
        {
            optionName = pair.Key;
            return true;
        }
        optionName = string.Empty;
        return false;
    }


    public bool IsScalarOption(int index, out string optionName, out string optionValue )
    {
        if (TryGetOption<KeyValuePair<string, string>>(index, out var pair))
        {
            optionName = pair.Key;
            optionValue = pair.Value;
            return true;
        }

        optionName = string.Empty;
        optionValue = string.Empty;
        return false;
    }


    public bool IsCollectionOption(int index, out string optionName, out string[] optionValues)
    {
        if (TryGetOption<KeyValuePair<string, string[]>>(index, out var pair))
        {
            optionName = pair.Key;
            optionValues = pair.Value.ToArray();
            return true;
        }

        optionName = string.Empty;
        optionValues = [];
        return false;
    }

    public bool IsKeyValueOption(
        int index, 
        out string optionName, 
        out string optionKey, 
        out string optionValue)
    {
        if (TryGetOption<KeyValuePair<string, KeyValuePair<string, string>>>(index, out var pair))
        {
            optionName = pair.Key;
            optionKey = pair.Value.Key;
            optionValue = pair.Value.Value;
            return true;
        }

        optionName = string.Empty;
        optionKey = string.Empty;
        optionValue = string.Empty;
        return false;
    }


    public bool IsKeyCollectionOption(
        int index,
        out string optionName,
        out string optionKey,
        out string[] optionValue)
    {
        if (TryGetOption<KeyValuePair<string, KeyValuePair<string, string[]>>>(index, out var pair))
        {
            optionName = pair.Key;
            optionKey = pair.Value.Key;
            optionValue = pair.Value.Value;
            return true;
        }

        optionName = string.Empty;
        optionKey = string.Empty;
        optionValue = [];
        return false;
    }


    public bool IsKeyFlagOption(
        int index,
        out string optionName,
        out string optionKey)
    {
        if (TryGetOption<KeyValuePair<string, KeyValuePair<string, Unit>>>(index, out var pair))
        {
            optionName = pair.Key;
            optionKey = pair.Value.Key;
            return true;
        }

        optionName = string.Empty;
        optionKey = string.Empty;
        return false;
    }


    public string Decode(string input)
    {
        int maxCycles = 1000;
        int cycle = 0;

        while (cycle++ < maxCycles)
        {
            int decodedCount = 0;

            foreach (var encoding in _encodings)
            {
                if (input.Contains(encoding.Key))
                {
                    input = input.Replace(encoding.Key, encoding.Value);
                    decodedCount++;
                }
            }

            if (decodedCount == 0)
            {
                return input;
            }
        }

        throw new InvalidOperationException("The decoding operation exceeded the maximum allowed iterations and was aborted.");

    }

    private bool TryGetOption<T>(int index, out T? option)
    {
        if (index < 0 || index >= _options.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index should be less than {_options.Length}.");
        }
        if (_options[index] is T matchedOption)
        {
            option = matchedOption;
            return true;
        }
        option = default;
        return false;
    }



    private static string EncodeCommandName(string commandLine, State state)
    {
        var match = @"(?xis-m)^(?<command>""[^""]+""|\S+)(?:\s+(?<parameters>.*))?$"
            .Convert(pattern => new Regex(pattern))
            .Match(commandLine.Trim());
        if (false == match.Success)
        {
            throw new FormatException();
        }

        var command = match.Groups["command"].Value.Trim('"');
        var parameters = match.Groups["parameters"].Value.Trim();
        if (File.Exists(command))
        {
            command = Path.GetFileName(command);
        }

        state.ProgramName = command;
        if (RegexUtils.HasWhiteSpaces(command))
        {
            var encoded = state.Encode(command);
            return $"{encoded} {parameters}";
        }

        return $"{command} {parameters}";
    }

    private string EncodeEnvVariables(string commandline, State state)
    {
        return @"%[\w_]+%"
            .Replace("$variable", @"%[\w_]+%")
            .Convert(pattern => new Regex(pattern))
            .Replace(commandline, match =>
            {

                var value = match.Value
                    .Trim('"')
                    .Trim('%');
                var envVariable = Environment.GetEnvironmentVariable(value);
                if (envVariable.IsPrintable())
                {
                    var key = state.Encode(envVariable!);
                    return key;

                }

                return match.Value;
            });
    }

    private string EncodeQuotedText(string commandLine, State state)
    {
        return @"""[^""]*"""
            .Convert(pattern => new Regex(pattern))
            .Replace(commandLine, match =>
            {
                var value = match.Value.Trim('"');
                return state.Encode(value);
            });
    }


    private string FormatMapOptions(string commandline, State state)
    {
        commandline = @"(?<option>-{1,}$option)\s*\[\s*(?<key>$key)\s*\]"
            .Replace("$option", @"[^\[\s]+")
            .Replace("$key", @"[^\[\]\s]+")
            .Convert(pattern => new Regex(pattern))
            .Replace(commandline, match => match.Result("${option}.${key}"));

        return commandline;
    }


    private string StripOptions(string commandline, State state)
    {
        commandline = @"(?xis-m)(?<=\s|^)
            (?<option>(?<name>-{1,2}[^\.\s]+) (?:\.(?<key>\S*))?)   
            (?:\s+(?<value>[^-\s]\S*))*"
            .Convert(pattern => new Regex(pattern))
            .Replace(commandline, match =>
            {
                var option = match.Groups["option"];
                var name = match.Groups["name"];
                var key = match.Groups["key"];
                var values = match.Groups["value"].Captures.Select(c => c.Value).ToArray();
                Debug.Assert(option.Success);
                Debug.Assert(name.Success);
                if (key.Success)
                {
                    state.AddMapOption(name.Value, key.Value, values);
                }
                else
                {
                    state.AddOption(name.Value, values);
                }
                
                return option.Value;
            });
        return commandline;
    }

    sealed class State(
        string commandLine,
        IDictionary<string, string> encodings)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly IDictionary<string, string> _encodings = encodings;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _commandLine = commandLine;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<object> _options = new();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _index = 0;

        public string ProgramName { get; set; } = String.Empty;

        public string Encode(string value)
        {
            var key = NextUniqueKey();
            _encodings.Add(key, value);
            return key;
        }

        public ImmutableArray<object> Options => [.._options];

        private string NextUniqueKey()
        {
            var key = $"{{{_index++}}}";
            int attempt = 0;
            while (_commandLine.Contains(key))
            {
                key = $"{{{Guid.NewGuid():N}}}";
                if ((attempt++) > 10)
                {
                    throw new InvalidOperationException();
                }
            }

            return key;
        }

        public void AddMapOption(string name, string key, string[] values)
        {
            Debug.Assert(name.IsPrintable());
            Debug.Assert(key.IsPrintable());
            if (values.Length == 0)
            {
                _options.Add(KeyValuePair.Create(
                    name,
                    KeyValuePair.Create(key, Unit.Default)));
            }
            else if (values.Length == 1)
            {
                _options.Add(KeyValuePair.Create(
                    name,
                    KeyValuePair.Create(key, values.Single())));
            }
            else
            {
                _options.Add(KeyValuePair.Create(
                    name,
                    KeyValuePair.Create(key, values)));
            }

        }

        public void AddOption(string name, string[] values)
        {
            if (values.Length == 0)
            {
                _options.Add(KeyValuePair.Create(name, Unit.Default));
            }
            else if (values.Length == 1)
            {
                _options.Add(KeyValuePair.Create(name, values.Single()));
            }
            else
            {
                _options.Add(KeyValuePair.Create(name, values));
            }
        }
    }

}