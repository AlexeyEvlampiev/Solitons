using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Solitons.CommandLine;

public interface ICliOptionMetadata
{
    /// <summary>
    /// Comma-separated list of all options.
    /// </summary>
    string OptionNamesCsv { get; }

    /// <summary>
    /// Specification of options as used in the CLI.
    /// </summary>
    string OptionPipeAliases { get; }

    /// <summary>
    /// Description of the CLI options.
    /// </summary>
    string Description { get; }

    IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// List of short option names.
    /// </summary>
    IReadOnlyList<string> ShortOptionNames { get; }

    /// <summary>
    /// List of long option names.
    /// </summary>
    IReadOnlyList<string> LongOptionNames { get; }

    bool AllowsCsv { get; }
    bool CanAccept(Type optionType, out TypeConverter converter);
    StringComparer GetValueComparer();

}