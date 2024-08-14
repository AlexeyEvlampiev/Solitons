using System;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliOptionInfo : CliParameterInfo
{
    public CliOptionInfo(ParameterInfo parameter) : base(parameter)
    {
        var attribute = Metadata
            .OfType<CliOptionAttribute>()
            .Single();
        var converter = attribute.GetCustomTypeConverter();

        if (converter is not null &&
            converter.CanConvertTo(this.ParameterType) == false)
        {
            var method = (MethodInfo)parameter.Member;
            throw new InvalidOperationException(
                $"The parameter '{parameter.Name}' in method '{method.Name}' is of type '{ParameterType}', " +
                $"which is not supported by the type converter associated with the '{attribute.GetType().Name}' attribute. " +
                $"Ensure that the parameter type is correct and compatible with the converter.");
        }
    }
}