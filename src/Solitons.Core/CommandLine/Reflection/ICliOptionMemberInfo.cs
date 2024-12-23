namespace Solitons.CommandLine.Reflection;

public interface ICliOptionMemberInfo
{
    bool IsMatch(string optionName);
}