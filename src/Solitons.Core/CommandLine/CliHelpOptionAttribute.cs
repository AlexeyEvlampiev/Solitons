using System.Text.RegularExpressions;

namespace Solitons.CommandLine;
internal sealed class CliHelpOptionAttribute() : CliOptionAttribute(HelpFlagExpression)
{
    private const string HelpFlagExpression = "--help|-h|-?";
    public static bool IsMatch(string commandLine)
    {
        var pattern = Regex
            .Replace(HelpFlagExpression, @"\?", "\\?")
            .Convert(p => $@"(?<=\s|^)(?:{p})(?=\s|$)");
        return Regex.IsMatch(commandLine, pattern);
    }
}