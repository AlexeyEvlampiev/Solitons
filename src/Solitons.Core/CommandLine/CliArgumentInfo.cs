using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliArgumentInfo : CliParameterInfo
{
    private readonly CliAction _action;
    private readonly CliRouteArgumentAttribute _metadata;

    public CliArgumentInfo(ParameterInfo parameter, CliAction action, CliRouteArgumentAttribute metadata) 
        : base(parameter)
    {
        ParameterInfo = parameter;
        _action = action;
        _metadata = metadata;
    }

    public ParameterInfo ParameterInfo { get; }
    public string ArgumentRole => _metadata.ArgumentRole;



    public string GetExpressionGroup() => Name;
}