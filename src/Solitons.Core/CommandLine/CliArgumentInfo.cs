using Solitons.CommandLine.Common;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliArgumentInfo : CliParameterInfo, ICliCommandSegment
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

    public string BuildPattern()
    {
        var index = _action.IndexOf(this);

        var result = _action
            .CommandSegments
            .Skip(index + 1)
            .OfType<CliSubCommand>()
            .Select(s => s.BuildPattern())
            .Select(p => $"(?:{p})")
            .Join("|")
            .Convert(p => p.IsNullOrWhiteSpace() ? "\\-" : $"\\-|{p}")
            .Convert(p => $"(?!{p})")
            .Convert(p => $"{p}(?<{this.Name}>\\S+)");
        return result;
    }

    public string GetExpressionGroup() => Name;
}