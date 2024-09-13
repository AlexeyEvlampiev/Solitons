using System;
using System.ComponentModel;

namespace Solitons.CommandLine;

internal sealed record CliFlagOptionTypeDescriptor(Type FlagType) : CliOptionTypeDescriptor
{
    public override TypeConverter GetDefaultTypeConverter() => FlagType == typeof(CliFlag) 
        ? new CliFlagConverter() 
        : new UnitConverter();

    public override string CreateRegularExpression(string regexGroupName, string pipeExpression) => 
        $@"(?<{regexGroupName}>{pipeExpression})";
}