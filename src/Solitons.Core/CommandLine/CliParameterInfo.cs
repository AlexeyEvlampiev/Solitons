using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal abstract class CliParameterInfo( ParameterInfo parameter) 
    : CliOperandInfo(parameter)
{
    public bool HasDefaultValue(out object? defaultValue)
    {
        if (parameter.HasDefaultValue)
        {
            defaultValue = parameter.DefaultValue;
            return true;
        }
        defaultValue = false;
        return parameter.HasDefaultValue;
    }

    public object? GetValue(Match match, CliTokenSubstitutionPreprocessor preprocessor)
    {
        if (FindValue(match, preprocessor, out var value))
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