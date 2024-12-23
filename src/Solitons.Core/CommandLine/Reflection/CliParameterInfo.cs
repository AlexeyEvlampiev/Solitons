using System.Reflection;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal abstract class CliParameterInfo(ParameterInfo parameter) : ParameterInfoDecorator(parameter), ICliOptionMemberInfo
{
    public abstract object Parse(string arg);
    public bool IsMatch(string optionName)
    {
        return false;
    }
}