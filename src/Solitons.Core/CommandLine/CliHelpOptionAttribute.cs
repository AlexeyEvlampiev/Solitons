using System.Text.RegularExpressions;

namespace Solitons.CommandLine;
internal sealed class CliHelpOptionAttribute() : CliOptionAttribute("--help|-h|-?")
{
    public static bool IsMatch(string commandLine)
    {
        var help = new CliHelpOptionAttribute();
        return Regex.IsMatch(commandLine, $@"(?:{help.OptionSpecification})");
    }
}