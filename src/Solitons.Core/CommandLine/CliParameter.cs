using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal abstract class CliParameter(ParameterInfo parameter) : CliOperand(parameter)
{
    public bool HasDefaultValue(out object defaultValue)
    {
        defaultValue = parameter.DefaultValue ?? false;
        return parameter.HasDefaultValue;
    }

    public object? GetValue(Match match)
    {
        if (FindValue(match, out var value))
        {
            return value;
        }

        if (HasDefaultValue(out value))
        {
            return value;
        }

        if (IsOptional)
        {
            return null;
        }

        throw new NotImplementedException();
    }
}