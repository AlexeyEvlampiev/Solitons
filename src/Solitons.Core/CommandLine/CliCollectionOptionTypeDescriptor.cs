using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;

namespace Solitons.CommandLine;

internal sealed record CliCollectionOptionTypeDescriptor(Type ConcreteType, Type ItemType) : CliOptionTypeDescriptor
{
    public static readonly IReadOnlyList<Type> SupportedGenericTypes = new[]
    {
        typeof(List<>),
        typeof(Stack<>),
        typeof(Queue<>),
        typeof(HashSet<>)
    }.AsReadOnly();

    private static readonly CliCollectionOptionTypeDescriptor DefaultDescriptor =
        new CliCollectionOptionTypeDescriptor(typeof(object[]), typeof(object));

    public static bool IsMatch(
        Type optionType, 
        out CliCollectionOptionTypeDescriptor descriptor)
    {
        descriptor = DefaultDescriptor;
        if (optionType == typeof(string) ||
            optionType == typeof(Unit) ||
            optionType == typeof(CliFlag) ||
            optionType.IsEnum ||
            typeof(IDictionary).IsAssignableFrom(optionType) ||
            typeof(IEnumerable).IsAssignableFrom(optionType) == false)
        {
            Debug.WriteLine($"{optionType} is not a collection or an incompatible type.");
            return false;
        }


        if (optionType.IsArray)
        {
            var elementType = optionType.GetElementType()!;
            descriptor = new CliCollectionOptionTypeDescriptor(optionType, elementType);
            return true;
        }

        if (optionType.IsGenericType == false ||
            optionType.GetGenericArguments().Length != 1)
        {
            return false;
        }

        Debug.Assert(optionType.IsGenericType);
        Debug.Assert(optionType.GetGenericArguments().Length == 1);

        var it = optionType.GetGenericArguments()[0];
        foreach (var genericType in SupportedGenericTypes)
        {
            var supportedType = genericType.MakeGenericType(it);
            if (optionType.IsAssignableFrom(supportedType))
            {
                descriptor = new CliCollectionOptionTypeDescriptor(supportedType, it);
                return true;
            }
        }

        return false;
    }


    public override TypeConverter GetDefaultTypeConverter() => TypeDescriptor.GetConverter(ItemType);

    public override string CreateRegularExpression(string regexGroupName, string pipeExpression) =>
        $@"(?:{pipeExpression})\s*(?<{regexGroupName}>(?:[^\s-]\S*)?)";

}