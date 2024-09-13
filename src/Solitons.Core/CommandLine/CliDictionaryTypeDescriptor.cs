using System;
using System.ComponentModel;

namespace Solitons.CommandLine;

internal sealed record CliDictionaryTypeDescriptor(Type ConcreteType, Type ValueType, bool AcceptsCustomStringComparer) : CliOptionTypeDescriptor
{
    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ValueType);

    public override string CreateRegularExpression(string regexGroupName, string pipeExpression) =>
        $@"(?:{pipeExpression})(?:$dot-notation|$accessor-notation)"
            .Replace(@"$dot-notation", @$"\.(?<{regexGroupName}>(?:\S+\s+[^\s-]\S*)?)")
            .Replace(@"$accessor-notation", @$"(?<{regexGroupName}>(?:\[\S+\]\s+[^\s-]\S*)?)");
}