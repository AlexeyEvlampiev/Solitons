using System.Collections.Immutable;

namespace Solitons.CommandLine.Reflection;

public interface ICliOptionMemberInfo
{
    string Name { get; }

    string PipeSeparatedAliases { get; }
    bool IsMatch(string optionName);

    bool IsOptional { get; }

    object? DefaultValue { get; }

    ImmutableArray<string> Aliases { get; }
    string Description { get; }


    public sealed bool IsIn(CliCommandLine commandLine)
    {
        //Debug.WriteLine(Name);
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