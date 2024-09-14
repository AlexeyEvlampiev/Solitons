using System;
using System.ComponentModel;
using System.Reactive;

namespace Solitons.CommandLine;

internal sealed record CliFlagOptionTypeDescriptor(Type FlagType) : CliOptionTypeDescriptor
{
    private static readonly CliFlagOptionTypeDescriptor Default = new(typeof(CliFlag));
    public override TypeConverter GetDefaultTypeConverter() => FlagType == typeof(CliFlag) 
        ? new CliFlagConverter() 
        : new UnitConverter();

    public override string CreateRegularExpression(string regexGroupName, string pipeExpression) => 
        $@"(?<{regexGroupName}>{pipeExpression})";

    public static bool IsMatch(Type optionType, out CliFlagOptionTypeDescriptor flag)
    {
        flag = Default;
        if (optionType == typeof(CliFlag))
        {
            flag = new CliFlagOptionTypeDescriptor(typeof(CliFlag));
            return true;    
        }
        if (optionType == typeof(Unit))
        {
            flag = new CliFlagOptionTypeDescriptor(typeof(Unit));
            return true;
        }
        return false;
    }
}