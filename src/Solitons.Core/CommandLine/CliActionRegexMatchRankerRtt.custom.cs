using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal partial class CliActionRegexMatchRankerRtt
{
    private readonly Regex _regex;
    private readonly string _executableGroupName;
    private readonly string _optimalMatchGroupName;


    private CliActionRegexMatchRankerRtt(ICliActionRegexMatchProvider provider)
    {
        var postfix = GetType().GUID.ToString("N");
        _executableGroupName = $"executable_{postfix}";
        _optimalMatchGroupName = $"optimal_{postfix}";

        CommandSegmentRegularExpressions = provider
            .GetCommandSegments()
            .Select(segment =>
            {
                if (segment is ICliSubCommandInfo cmd)
                {
                    return cmd
                        .GetAliases()
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(a => a.ToLower().Trim())
                        .OrderByDescending(a => a.Length)
                        .Join("|")
                        .DefaultIfNullOrWhiteSpace("(?:$)*");
                }

                if (segment is ICliArgumentInfo arg)
                {

                }

                throw new InvalidOperationException("Oops...");
            })
            .ToArray();


        var pattern = ToString();
        Pattern = pattern;
        _regex = new Regex(pattern,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Singleline);

    }

    [DebuggerStepThrough]
    public static CliActionRegexMatchRankerRtt Create(ICliActionRegexMatchProvider provider) => new(provider);

    public string Pattern { get; }
    private IReadOnlyList<string> CommandSegmentRegularExpressions { get; }
    public IReadOnlyList<string> OptionRegularExpressions { get; }

    public int Rank(string commandLine)
    {
        var match = _regex.Match(commandLine);
        int rank = 0;
        foreach (Group group in match.Groups)
        {
            if (group.Success)
            {
                rank += group.Name.Equals(_optimalMatchGroupName, StringComparison.Ordinal)
                    ? 100
                    : 1;
            }
        }

        return rank;
    }


}