using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solitons.Collections;


/// <summary>
/// Provides utility methods for building collections and dictionaries from 
/// specified types, items, and optionally comparers.
/// </summary>
public static class CollectionBuilder
{
    /// <summary>
    /// Builds a strongly-typed collection based on the specified collection type, 
    /// a sequence of items, and an optional equality comparer.
    /// </summary>
    /// <param name="collectionType">The type of collection to create. This type must 
    /// be a concrete collection or support IEnumerable, IList, ISet, or IReadOnlyCollection interfaces.</param>
    /// <param name="collectionItems">The sequence of items to populate the collection with.</param>
    /// <param name="comparer">An optional equality comparer used to compare elements in the collection.</param>
    /// <returns>A strongly-typed collection containing the specified items.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="collectionType"/> or <paramref name="collectionItems"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the specified collection type is invalid 
    /// or if the items cannot be cast to the expected type for the collection.</exception>
    public static IEnumerable BuildCollection(
        Type collectionType,
        IEnumerable<object> collectionItems,
        IEqualityComparer? comparer = null)
    {
        collectionType = ThrowIf.ArgumentNull(collectionType);
        var items = ThrowIf
            .ArgumentNull(collectionItems)
            .ToArray();

        collectionType = collectionType
            .Convert(t => GetConcreteCollectionType(t, items));

        var typedItems = ToTypedArray(collectionType, items);

        if (collectionType.IsArray)
        {
            return typedItems;
        }

        try
        {
            var itemType = collectionType.GetGenericArguments().Single();

            if (comparer is not null)
            {
                var ctor = collectionType
                    .GetConstructor([
                        typeof(IEnumerable<>).MakeGenericType(itemType),
                        typeof(IEqualityComparer<>).MakeGenericType([itemType])
                    ]);
                if (ctor is not null)
                {
                    return (IEnumerable)ctor.Invoke([typedItems, comparer]);
                }

                ctor = collectionType
                    .GetConstructor([
                        typeof(IEqualityComparer<>).MakeGenericType([itemType])
                    ]);
                if (ctor is not null)
                {
                    var collection = ctor.Invoke([comparer]) as IEnumerable ?? throw new InvalidOperationException();
                    PopulateCollection(collection, typedItems);
                    return collection;
                }

                throw new InvalidOperationException("oops...");
            }
            else
            {
                var ctor = collectionType
                    .GetConstructor([
                        typeof(IEnumerable<>).MakeGenericType(itemType)
                    ]);
                if (ctor is not null)
                {
                    return (IEnumerable)ctor.Invoke([typedItems]);
                }

                if (typeof(ReadOnlyCollection<>).MakeGenericType([itemType]) == collectionType)
                {
                    var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType), typedItems);
                    return Activator.CreateInstance(collectionType, [list]) as IEnumerable ?? throw new InvalidOperationException();
                }
                var collection = Activator.CreateInstance(collectionType) as IEnumerable ?? throw new InvalidOperationException();
                PopulateCollection(collection, typedItems);
                return collection;
            }
            
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Collection creation failed: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a dictionary of the specified type, optionally using an equality comparer.
    /// </summary>
    /// <param name="dictionaryType">The type of the dictionary to create. 
    /// This type must implement <see cref="IDictionary"/> or <see cref="IDictionary{TKey,TValue}"/>.</param>
    /// <param name="comparer">An optional equality comparer to compare dictionary keys.</param>
    /// <returns>A dictionary of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionaryType"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the specified type does not implement 
    /// <see cref="IDictionary"/> or <see cref="IDictionary{TKey,TValue}"/>, or if a valid constructor cannot be found.</exception>
    public static IDictionary CreateDictionary(Type dictionaryType, IEqualityComparer? comparer = null)
    {
        if (dictionaryType.GetGenericTypeDefinition() != typeof(IDictionary<,>) &&
            false == typeof(IDictionary).IsAssignableFrom(dictionaryType))
        {
            throw new InvalidOperationException("The provided type does not implement IDictionary.");
        }

        Type[]? genericArgs = dictionaryType.IsGenericType
            ? dictionaryType.GetGenericArguments()
            : null;

        // For IDictionary<TKey, TValue>
        if (genericArgs != null && genericArgs.Length == 2)
        {
            Type keyType = genericArgs[0];
            Type valueType = genericArgs[1];

            // Create a concrete Dictionary<TKey, TValue> if an interface was passed
            if (dictionaryType.IsInterface)
            {
                dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            }

            // Handle the comparer if provided
            if (comparer != null)
            {
                var ctorWithComparer = dictionaryType.GetConstructor([
                    typeof(IEqualityComparer<>).MakeGenericType(keyType)
                ]);
                if (ctorWithComparer != null)
                {
                    return (IDictionary)ctorWithComparer.Invoke([comparer]);
                }

                throw new InvalidOperationException("Oops...");
            }

            // Default constructor
            var defaultCtor = dictionaryType.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
            {
                return (IDictionary)Activator.CreateInstance(dictionaryType)!;
            }

            throw new InvalidOperationException(
                $"No constructor accepting IEqualityComparer<{keyType.Name}> found for the dictionary type.");
        }

        // For non-generic IDictionary
        if (typeof(IDictionary).IsAssignableFrom(dictionaryType) && !dictionaryType.IsGenericType)
        {
            // If comparer is provided for non-generic dictionaries, it can't be used, so just create an empty dictionary
            var defaultCtor = dictionaryType.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
            {
                return (IDictionary)Activator.CreateInstance(dictionaryType)!;
            }

            throw new InvalidOperationException("No valid constructor found for the dictionary type.");
        }

        throw new InvalidOperationException("Unsupported dictionary type.");
    }

    /// <summary>
    /// Converts a sequence of untyped objects to a strongly-typed array.
    /// </summary>
    /// <param name="collectionType">The type of the collection to create the array for.</param>
    /// <param name="items">The sequence of untyped objects to convert.</param>
    /// <returns>A strongly-typed array of the specified element type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the element type cannot be determined 
    /// or if an item in the sequence cannot be cast to the expected type.</exception>
    private static Array ToTypedArray(Type collectionType, object[] items)
    {
        // Determine the element type, considering array and generic collections
        var elementType = collectionType.IsArray
            ? collectionType.GetElementType()
            : collectionType.IsGenericType && collectionType.GenericTypeArguments.Length == 1
                ? collectionType.GenericTypeArguments[0]
                : throw new InvalidOperationException("Unable to determine the collection's element type.");

        // Check if elementType is valid
        if (elementType == null)
        {
            throw new InvalidOperationException("Element type could not be determined.");
        }

        // Create a destination array of the required element type
        var destination = Array.CreateInstance(elementType, items.Length);

        // Ensure the source items are compatible with the element type
        for (int i = 0; i < items.Length; i++)
        {
            if (!elementType.IsInstanceOfType(items[i]))
            {
                throw new InvalidOperationException($"Item at index {i} is not of the expected type {elementType}.");
            }

            destination.SetValue(items[i], i);
        }

        return destination;
    }

    /// <summary>
    /// Populates a collection with the provided items by invoking the appropriate 
    /// insertion method (e.g., Add, Enqueue, Push).
    /// </summary>
    /// <param name="collection">The collection to populate.</param>
    /// <param name="items">The items to add to the collection.</param>
    /// <exception cref="InvalidOperationException">Thrown if no suitable method is found to populate the collection 
    /// or if an item cannot be cast to the expected type for the collection.</exception>
    private static void PopulateCollection(IEnumerable collection, IEnumerable items)
    {
        // Retrieve the collection's type
        var collectionType = collection.GetType();

        // Try to find the Add, Enqueue, or Push method that accepts a single parameter
        var insertionMethods = new[] { "Add", "Enqueue", "Push" };
        MethodInfo addMethod = insertionMethods
            .Select(methodName => collectionType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public))
            .Where(mi =>
            {
                if (mi is null)
                {
                    return false;
                }

                var parameters = mi.GetParameters();
                if (parameters.Length != 1)
                {
                    return false;
                }

                return true;
            })
            .FirstOrDefault() ?? throw new InvalidOperationException("No suitable method found to populate the collection.");

        var parameterType = addMethod.GetParameters()[0].ParameterType;
        var args = new object[1];
        foreach (var item in items)
        {
            if (!parameterType.IsInstanceOfType(item))
            {
                throw new InvalidOperationException($"Item of type {item.GetType()} is not compatible with expected type {parameterType}.");
            }
            args[0] = item;
            addMethod.Invoke(collection, args);
        }
    }

    /// <summary>
    /// Determines the concrete collection type to instantiate based on the specified collection type and the items.
    /// </summary>
    /// <param name="collectionType">The type of the collection to determine.</param>
    /// <param name="items">The items to populate the collection with.</param>
    /// <returns>A concrete collection type that is compatible with the specified type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the item types are incompatible with the collection type.</exception>
    private static Type GetConcreteCollectionType(Type collectionType, IReadOnlyList<object> items)
    {
        {
            if (collectionType.IsGenericType || collectionType.IsArray)
            {
                var itemType = collectionType.IsGenericType
                    ? collectionType.GetGenericArguments()[0]
                    : collectionType.GetElementType()
                    ?? throw new InvalidOperationException("Could not determine the collection's item type.");
                var csv = items
                    .Skip(itemType.IsInstanceOfType)
                    .Select(item => item.GetType())
                    .Distinct()
                    .Select(t => t.FullName)
                    .Take(5)!
                    .Join(", ");
                if (csv.IsPrintable())
                {
                    throw new InvalidOperationException($"Incompatible collection item types found: {csv}");
                }

                if (collectionType.IsArray)
                {
                    return collectionType;
                }

                if (collectionType.IsInterface)
                {
                    //TODO: extend logic below to handle other cases when an interface can be instantiated correctly and safely
                    if (typeof(ISet<>).MakeGenericType([itemType]) == collectionType ||
                        typeof(IReadOnlySet<>).MakeGenericType([itemType]) == collectionType)
                    {
                        return typeof(HashSet<>).MakeGenericType([itemType]);
                    }

                    if (typeof(IList<>).MakeGenericType([itemType]) == collectionType ||
                        typeof(IReadOnlyList<>).MakeGenericType([itemType]) == collectionType ||
                        typeof(IReadOnlyCollection<>).MakeGenericType([itemType]) == collectionType ||
                        typeof(ICollection<>).MakeGenericType([itemType]) == collectionType)
                    {
                        return typeof(List<>).MakeGenericType([itemType]);
                    }

                    if (typeof(IEnumerable<>).MakeGenericType([itemType]) == collectionType)
                    {
                        return itemType.MakeArrayType();
                    }
                }
            }

            return collectionType;
        }
    }
}
