﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.ZapCli;

internal partial class ZapCliActionRegexRtt
{
    private const string ProgramGroupName = "ID382292c985c64db183d4382216a195ef";
    private const string UnrecognizedCommandTokenName = "ID49ac98816b734782bcf61665cc0e53f0";

    public record Option(string Name, string Pattern);

    private readonly CliAction _action;


    //[DebuggerNonUserCode]
    private ZapCliActionRegexRtt(CliAction action, ZapCliActionRegexRttMode mode)
    {
        Mode = mode;
        _action = action;
        SubCommands = _action.CommandSegments.OfType<CliSubCommand>();
        CommandSegments = _action
            .CommandSegments
            .Where(segment =>
            {
                if (segment is CliArgument)
                {
                    return true;
                }

                if (segment is CliSubCommand cmd)
                {
                    return cmd.PrimaryName.IsPrintable();
                }

                throw new InvalidOperationException();
            })
            .ToArray();
    }
    
    


    internal static string Build(CliAction action, ZapCliActionRegexRttMode mode) => new ZapCliActionRegexRtt(action, mode)
        .ToString()
#if DEBUG
        .Convert(p => Regex.Replace(p, @"((?<=\n)[^\S\n]+\n+)+", ""))
#endif
        .Trim();


    internal ZapCliActionRegexRttMode Mode { get; }

    private IEnumerable<ICliCommandSegment> CommandSegments { get; }

    private IEnumerable<ICliCommandSegment> SubCommands { get; } 

    public bool IsDefaultMode => Mode == ZapCliActionRegexRttMode.Default;
    public bool IsSimilarityMode => Mode == ZapCliActionRegexRttMode.Similarity;

    private IEnumerable<Option> Options => _action
        .Operands
        .Where(operand => operand is not CliArgument)
        .Select(operand => new Option(operand.Name, operand.NamedGroupPattern));


    public static Group GetProgramGroup(Match match) => match.Groups[ProgramGroupName];

    public static Group GetUnmatchedParameterGroup(Match match) => match.Groups[UnrecognizedCommandTokenName];

    private string GetSegmentPattern(ICliCommandSegment segment) => segment.BuildPattern();

    private string GetSegmentGroupName(ICliCommandSegment segment) => segment.GetExpressionGroup();


    [Conditional("DEBUG")]
    private void Test(string pattern)
    {
        var _ = new Regex(pattern, RegexOptions.Compiled);
    }
}