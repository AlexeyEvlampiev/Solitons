using System.Collections.Immutable;

namespace Solitons.CommandLine.Reflection;

public interface ICliOptionMemberInfo
{
    bool IsMatch(string optionName);

    string PipeSeparatedAliases { get; }

    string OptionAliasesCsv { get; }

    bool IsOptional { get; }

    ImmutableArray<string> Aliases { get; }



    public sealed bool IsIn(CliCommandLine commandLine)
    {
        foreach (var option in commandLine.Options)
        {
            if (IsMatch(option.Name))
            {
                return true;
            }
        }

        return false;
    }


    bool IsNotIn(CliCommandLine commandLine) => (false == IsIn(commandLine));
}