using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;
internal sealed class CliHelpOptionAttribute() : CliOptionAttribute(HelpFlagExpression, HelpFlagDescription)
{
    private const string HelpFlagExpression = "--help|-h|-?";
    private const string HelpFlagDescription = "Displays the help information for commands and options.";
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

    public static bool IsGeneralHelpRequest(string commandLine)
    {
        return Regex.IsMatch(commandLine, @$"^\S+\s+{Pattern}\s*$");
    }

    [DebuggerStepThrough]
    public override bool CanAccept(Type optionType, out TypeConverter converter) => CanAcceptIfIsFlags(optionType, out converter);
}