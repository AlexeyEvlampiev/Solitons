﻿using System.Text.RegularExpressions;

namespace Solitons.CommandLine;
internal sealed class CliHelpOptionAttribute() : CliOptionAttribute(HelpFlagExpression)
{
    private const string HelpFlagExpression = "--help|-h|-?";
    private static readonly string Pattern;

    static CliHelpOptionAttribute()
    {
        Pattern = Regex
            .Replace(HelpFlagExpression, @"\?", "\\?")
            .Convert(p => $@"(?<=\s|^)(?:{p})(?=\s|$)");
    }


    public static bool IsMatch(string commandLine)
    {
        return Regex.IsMatch(commandLine, Pattern);
    }

    public static bool IsRootHelpCommand(string commandLine)
    {
        return Regex.IsMatch(commandLine, @$"^\S+\s+{Pattern}\s*$");
    }
}