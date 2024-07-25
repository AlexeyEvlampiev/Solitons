using System;
using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliScalarOperandTypeConverter : CliOperandTypeConverter
{
    private readonly string _parameterName;
    private readonly Type _type;
    private readonly TypeConverter _typeConverter;

    public CliScalarOperandTypeConverter(Type type, string parameterName)
        : base(false)
    {
        if (!IsScalar(type))
        {
            throw new ArgumentException("The type is not a scalar type.", nameof(type));
        }

        _type = type;
        _parameterName = parameterName;
        _typeConverter = TypeDescriptor.GetConverter(_type);
    }

    public static bool IsScalar(Type type)
    {
        return type == typeof(string) || 
               !typeof(IEnumerable).IsAssignableFrom(type);
    }

    protected override object Convert(Match match)
    {
        // Retrieve the value from the match using the parameter name
        var valueString = match.Groups[_parameterName].Value;

        if (string.IsNullOrEmpty(valueString))
        {
            throw new InvalidOperationException("The matched value is empty.");
        }

        // Convert the string value to the desired type using TypeConverter
        var convertedValue = _typeConverter.ConvertFromInvariantString(valueString);
        if (convertedValue == null)
        {
            throw new InvalidOperationException($"Unable to convert '{valueString}' to {_type}.");
        }

        return convertedValue;
    }

    public override string ToMatchPattern(string keyPattern)
    {
        var pattern = $@"{keyPattern}\s*(?<{_parameterName}>[^-]\S*)?";
        return pattern;
    }
}