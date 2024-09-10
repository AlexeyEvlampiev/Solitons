using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;

namespace Solitons.CommandLine;

/// <summary>
/// Provides utility methods for handling common command-line interface (CLI) operations.
/// </summary>
internal static class CliUtils
{
    public static CliOptionArity GetOptionArity(Type optionType)
    {
        optionType = Nullable.GetUnderlyingType(optionType) ?? optionType;
        return optionType switch
        {
            // Check if the type implements IDictionary<string, T>
            { } t when t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                    i.GetGenericArguments()[0] == typeof(string))
                => CliOptionArity.Dictionary,

            // Check if the type implements IEnumerable but is not a string
            { } t when typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string)
                => CliOptionArity.Collection,

            // Check if the type represents a Unit (assuming Unit is a specific type in your code)
            { } t when t == typeof(Unit)
                => CliOptionArity.Flag,

            // Default case: Scalar
            _ => CliOptionArity.Value,
        };
    }

    /// <summary>
    /// Gets the underlying type of the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="valueType">The type to examine.</param>
    /// <returns>
    /// If <paramref name="valueType"/> is a nullable type, the underlying type of the nullable type.
    /// If <paramref name="valueType"/> is <see cref="IEnumerable{T}"/>, the type of <c>T</c>.
    /// If <paramref name="valueType"/> is <see cref="IDictionary{TKey, TValue}"/> with <c>TKey</c> as <see cref="string"/>, the type of <c>TValue</c>.
    /// Otherwise, returns the original <paramref name="valueType"/>.
    /// </returns>
    /// <remarks>
    /// This method is useful for retrieving the core type in cases where the input type might be wrapped in a nullable,
    /// enumerable, or dictionary type. It is particularly useful in scenarios involving reflection or generic type handling.
    /// </remarks>
    public static Type GetUnderlyingType(Type valueType)
    {
        // Handle string explicitly
        if (valueType == typeof(string))
        {
            return valueType;
        }

        // Handle Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        // Handle IEnumerable<T>
        if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return underlyingType.GetGenericArguments()[0];
        }

        // Handle IDictionary<string, T>
        if (underlyingType.IsGenericType)
        {
            var genericTypeDefinition = underlyingType.GetGenericTypeDefinition();

            // Check if it's either IDictionary<,> or Dictionary<,>, or any type implementing IDictionary<,>
            if (genericTypeDefinition == typeof(Dictionary<,>) ||
                typeof(IDictionary<,>).IsAssignableFrom(genericTypeDefinition))
            {
                var genericArguments = underlyingType.GetGenericArguments();
                if (genericArguments.Length == 2 && genericArguments[0] == typeof(string))
                {
                    return genericArguments[1]; // Return the value type if the key is string
                }
            }
        }

        return underlyingType;
    }
}