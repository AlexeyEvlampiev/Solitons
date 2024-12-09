﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Solitons;

public static partial class Extensions
{
    /// <summary>
    /// Retrieves the key and value types of all generic <see cref="IDictionary{TKey, TValue}"/>
    /// interfaces implemented by the specified type.
    /// </summary>
    /// <param name="type">The type to inspect for generic dictionary interfaces.</param>
    /// <returns>
    /// An enumerable collection of key-value pairs, where each pair represents
    /// the key and value types of a generic <see cref="IDictionary{TKey, TValue}"/> interface
    /// implemented by the type.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <c>null</c>.</exception>
    public static IEnumerable<KeyValuePair<Type, Type>> GetGenericDictionaryArgumentTypes(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType &&
                ReferenceEquals(interfaceType.GetGenericTypeDefinition(), typeof(IDictionary<,>)))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                yield return new KeyValuePair<Type, Type>(genericArguments[0], genericArguments[1]);
            }
        }
    }


    /// <summary>
    /// Retrieves the element types of all generic <see cref="IEnumerable{T}"/>
    /// interfaces implemented by the specified type.
    /// </summary>
    /// <param name="type">The type to inspect for generic enumerable interfaces.</param>
    /// <returns>
    /// An enumerable collection of element types for each <see cref="IEnumerable{T}"/>
    /// interface implemented by the type.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <c>null</c>.</exception>
    public static IEnumerable<Type> GetGenericEnumerableArgumentTypes(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        foreach (var @interface in type.GetInterfaces())
        {
            if (@interface.IsGenericType &&
                ReferenceEquals(@interface.GetGenericTypeDefinition(), typeof(IEnumerable<>)))
            {
                yield return @interface.GetGenericArguments()[0];
            }
        }
    }
}