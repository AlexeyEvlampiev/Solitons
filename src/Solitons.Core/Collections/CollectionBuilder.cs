using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solitons.Collections;


public static class CollectionBuilder
{
    public static IEnumerable CreateInstance(
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
