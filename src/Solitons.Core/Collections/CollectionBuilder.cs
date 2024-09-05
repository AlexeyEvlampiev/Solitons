using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solitons.Collections;

/// <summary>
/// Provides functionality to dynamically construct and populate collections of a specific type.
/// </summary>
public sealed class DynamicCollectionFactory
{
    private readonly object _collection;
    private readonly Type _itemType;
    private readonly Action<object> _appender;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicCollectionFactory"/> class.
    /// </summary>
    /// <param name="collectionType">The type of the collection to construct and populate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collectionType"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="collectionType"/> is not a collection or does not have a valid add method.</exception>
    public DynamicCollectionFactory(Type collectionType)
    {
        ThrowIf.ArgumentNull(collectionType);

        // Ensure the type is a generic collection type (ICollection<T>, IList<T>, etc.)
        Type? interfaceType = collectionType
            .GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType &&
                                 (typeof(ICollection<>).IsAssignableFrom(t.GetGenericTypeDefinition()) ||
                                  typeof(IEnumerable<>).IsAssignableFrom(t.GetGenericTypeDefinition())));

        if (interfaceType == null)
            throw new ArgumentException($"{collectionType.FullName} is not a supported collection type.", nameof(collectionType));

        // Set the item type for the collection
        _itemType = interfaceType.GetGenericArguments()[0];

        // Instantiate the collection
        _collection = CreateCollectionInstance(collectionType);

        // Find the appropriate "add" method (Add, Enqueue, Push)
        _appender = GetAppenderMethod(collectionType);
    }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add to the collection.</param>
    /// <returns>The current <see cref="DynamicCollectionFactory"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the item is not of the expected type.</exception>
    public DynamicCollectionFactory Add(object item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (!_itemType.IsInstanceOfType(item))
            throw new ArgumentException($"Item must be of type {_itemType.FullName}", nameof(item));

        _appender(item);
        return this;
    }

    /// <summary>
    /// Builds the collection and returns the populated instance.
    /// </summary>
    /// <returns>The populated collection object.</returns>
    public object Build() => _collection;

    /// <summary>
    /// Creates an instance of the given collection type using reflection.
    /// </summary>
    /// <param name="collectionType">The type of collection to instantiate.</param>
    /// <returns>An instance of the specified collection.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection cannot be instantiated.</exception>
    private static object CreateCollectionInstance(Type collectionType)
    {
        try
        {
            return Activator.CreateInstance(collectionType) ??
                   throw new ArgumentException($"Cannot instantiate collection type {collectionType.FullName}.", nameof(collectionType));
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Cannot instantiate collection type {collectionType.FullName}: {ex.Message}", nameof(collectionType), ex);
        }
    }

    /// <summary>
    /// Retrieves the appropriate "add" method for the collection (Add, Enqueue, or Push).
    /// </summary>
    /// <param name="collectionType">The type of the collection.</param>
    /// <returns>An action to append an item to the collection.</returns>
    /// <exception cref="ArgumentException">Thrown when no valid add method is found.</exception>
    private Action<object> GetAppenderMethod(Type collectionType)
    {
        // Possible add method names
        var methodNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(IList.Add),
                nameof(Queue.Enqueue),
                nameof(Stack.Push)
            };

        // Find a method that matches the expected signature: public, instance, takes a single argument of _itemType
        MethodInfo? method = collectionType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => methodNames.Contains(m.Name))
            .FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == _itemType;
            });

        if (method == null)
            throw new ArgumentException($"No valid add method found in {collectionType.FullName}.", nameof(collectionType));

        // Return the method invocation as an action
        return item => method.Invoke(_collection, [item]);
    }
}
