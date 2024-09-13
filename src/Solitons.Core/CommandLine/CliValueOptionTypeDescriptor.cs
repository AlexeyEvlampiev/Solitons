using System;
using System.ComponentModel;

namespace Solitons.CommandLine;

internal sealed record CliValueOptionTypeDescriptor(Type ValueType) : CliOptionTypeDescriptor
{
    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ValueType);
    public override string CreateRegularExpression(string regexGroupName, string pipeExpression) => 
        $@"(?:{pipeExpression})\s*(?<{regexGroupName}>(?:[^\s-]\S*)?)";
}