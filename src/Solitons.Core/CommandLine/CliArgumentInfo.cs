using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliArgumentInfo : CliParameterInfo
{
    private readonly CliAction _action;
    private readonly CliArgumentAttribute _attribute;

    public CliArgumentInfo(ParameterInfo parameter, CliAction action, CliArgumentAttribute attribute) 
        : base(parameter)
    {
        ParameterInfo = parameter;
        _action = action;
        _attribute = attribute;
    }

    public ParameterInfo ParameterInfo { get; }
    public string ArgumentRole => _attribute.ArgumentRole;



    public string GetExpressionGroup() => Name;
}