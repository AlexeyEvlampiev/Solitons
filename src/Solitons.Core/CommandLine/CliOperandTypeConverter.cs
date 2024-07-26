﻿using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal abstract class CliOperandTypeConverter(bool allowsMultipleValues)
{
    public bool AllowsMultipleValues { get;  } = allowsMultipleValues;

    public object FromMatch(Match match)
    {
        if (false == match.Success)
        {
            throw new ArgumentException();
        }

        return Convert(match);
    }


    protected abstract object Convert(Match match);

    public abstract string ToMatchPattern(string keyPattern);

    

    public static CliOperandTypeConverter Create(Type type, string parameterType, TypeConverter? customTypeConverter)
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
            return new CliMapOperandTypeConverter(type, parameterType, customTypeConverter);
        }

        //TODO: if type is Dictionary<string, T> return CliMapOperandTypeConverter
        throw new NotSupportedException();
    }
}