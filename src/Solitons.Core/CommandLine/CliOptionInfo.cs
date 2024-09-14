using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Solitons.CommandLine;

internal abstract record CliOptionInfo
{
    internal sealed record Config
    {
        public required ICliOptionMetadata Metadata { get; init; }

        public required object? DefaultValue { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required Type OptionType { get; init; }

        public required bool IsRequired { get; init; }
    };

    private readonly object? _defaultValue;
    private readonly Lazy<string> _regularExpression;

    protected CliOptionInfo(Config config)
    {
        OptionMetadata = ThrowIf.ArgumentNull(config.Metadata);
        OptionType = Nullable.GetUnderlyingType(config.OptionType) ?? config.OptionType;
        _defaultValue = config.DefaultValue;
        Aliases = config.Metadata.Aliases;
        Description = config.Description;
        RegexMatchGroupName = $"option_{config.Name}_{Guid.NewGuid():N}";
        AliasPipeExpression = Aliases.Join("|");
        AliasCsvExpression = Aliases
            .OrderBy(alias => alias.StartsWith("--") ? 1 : 0)
            .ThenBy(alias => alias.Length)
            .Join(",");

        IsRequired = config.IsRequired;

        ThrowIf.NullOrWhiteSpace(AliasPipeExpression);
        var pipeExp = AliasPipeExpression.Replace("?", "[?]");
        _regularExpression = new Lazy<string>(() => BuildOptionRegularExpression(pipeExp));
    }

    public static CliOptionInfo Create(
        ICliOptionMetadata metadata,
        string name,
        object? defaultValue,
        string description,
        Type optionType,
        bool isRequired)
    {
        var config = new Config
        {
            Name = name,
            Description = description,
            Metadata = metadata,
            IsRequired = isRequired,
            OptionType = optionType,
            DefaultValue = defaultValue
        };

        CliOptionInfo? result = null;

        if (CliFlagOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliFlagOptionInfo);
        }
        else if (CliDictionaryOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliDictionaryOptionInfo);
        }
        else if (CliCollectionOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliCollectionOptionInfo);
        }
        else if (CliValueOptionInfo.IsMatch(config, out result))
        {
            Debug.Assert(result is CliValueOptionInfo);
        }
        else
        {
            if (config.Metadata.CanAccept(optionType, out var converter) && 
                converter.CanConvertFrom(typeof(string)))
            {
                throw new InvalidOperationException();
            }

            throw CliConfigurationException.NotSupportedOptionType();
        }

        return ThrowIf.NullReference(result);
    }

    public abstract object Deserialize(Group optionGroup, CliTokenDecoder decoder);

    protected abstract string BuildOptionRegularExpression(string pipeExp);

    public ICliOptionMetadata OptionMetadata { get; }

    public string RegularExpression => _regularExpression.Value;


    public bool IsRequired { get; }

    internal string RegexMatchGroupName { get; }

    public IReadOnlyList<string> Aliases { get; }

    public string Description { get; }

    public Type OptionType { get; }

    public string AliasPipeExpression { get; }
    public string AliasCsvExpression { get; }

    public object? Deserialize(Match commandLineMatch, CliTokenDecoder decoder)
    {
        Debug.Assert(commandLineMatch.Success);
        var group = commandLineMatch.Groups[RegexMatchGroupName];
        if (group.Success)
        {
            return this.Deserialize(group, decoder);
        }

        if (IsRequired)
        {
            CliExit.With($"{AliasPipeExpression} option is required.");
        }

        return _defaultValue;

    }
}