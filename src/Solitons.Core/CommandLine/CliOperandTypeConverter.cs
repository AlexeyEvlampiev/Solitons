using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal abstract class CliOperandTypeConverter(bool allowsMultipleValues)
{
    public bool AllowsMultipleValues { get;  } = allowsMultipleValues;

    [DebuggerStepThrough]
    public object FromMatch(
        Match match,
        CliTokenSubstitutionPreprocessor preprocessor)
    {
        if (false == match.Success)
        {
            throw new ArgumentException();
        }

        return Convert(match, preprocessor);
    }


    protected abstract object Convert(
        Match match,
        CliTokenSubstitutionPreprocessor preprocessor);

    public abstract string ToMatchPattern(string keyPattern);

    

    public static CliOperandTypeConverter Create(
        Type type, 
        string parameterType, 
        IReadOnlyList<object> metadata,
        TypeConverter? customTypeConverter)
    {
        if (CliFlagOperandTypeConverter.IsFlag(type))
        {
            return new CliFlagOperandTypeConverter(type, parameterType);
        }

        if (CliScalarOperandTypeConverter.IsScalar(type))
        {
            return new CliScalarOperandTypeConverter(type, parameterType, customTypeConverter);
        }

        if (CliMapOperandTypeConverter.IsMap(type))
        {
            return new CliMapOperandTypeConverter(type, parameterType, metadata, customTypeConverter);
        }

        //TODO: if type is Dictionary<string, T> return CliMapOperandTypeConverter
        throw new NotSupportedException();
    }
}