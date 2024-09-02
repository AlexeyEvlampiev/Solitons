using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal interface ICliActionSchema
{
    /// <summary>
    /// Matches the specified command line string against the schema.
    /// </summary>
    /// <param name="commandLine">The command line string to match.</param>
    /// <param name="preProcessor"></param>
    /// <param name="unrecognizedTokensHandler"></param>
    /// <returns>A <see cref="CliActionSchema.Match"/> object that contains information about the match.</returns>
    Match Match(
        string commandLine, 
        ICliTokenSubstitutionPreprocessor preProcessor,
        Action<ISet<string>> unrecognizedTokensHandler);

    bool IsMatch(string commandLine);

    /// <summary>
    /// Calculates the rank of the specified command line based on the schema.
    /// </summary>
    /// <param name="commandLine">The command line string to rank.</param>
    /// <returns>An integer representing the rank.</returns>
    int Rank(string commandLine);

    IEnumerable<CliActionSchema.ICommandSegment> CommandSegments { get; }
    IEnumerable<CliActionSchema.IOption> Options { get; }
    string CommandFullPath { get; }
    string CommandDescription { get; }
    CliCommandExampleAttribute[] Examples { get; }
    string GetHelpText();
}