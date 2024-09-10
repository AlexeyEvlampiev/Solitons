using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Solitons.Collections;

public sealed class CollectionBuilder_CreateInstance_Should
{
    [Theory]
    [InlineData(typeof(int[]), "1,2,3")]
    [InlineData(typeof(IEnumerable<int>), "1,2,3")]
    public void CreateArrays(Type type, string expectedItemsCsv)
    {
        var expectedItems = Regex
            .Split(expectedItemsCsv, @"\s*,\s*")
            .Where(i => i.IsPrintable())
            .Select(int.Parse)
            .ToArray();

        var result = CollectionBuilder.BuildCollection(type, expectedItems.Cast<object>());
        var actual = Assert.IsType<int[]>(result);
        Assert.Equal(expectedItems, actual);
    }

    [Theory]
    [InlineData(typeof(List<int>), "1,2,3")]
    [InlineData(typeof(IList<int>), "1,2,3")]
    [InlineData(typeof(IReadOnlyList<int>), "1,2,3")]
    [InlineData(typeof(IReadOnlyCollection<int>), "1,2,3")]
    public void CreateLists(Type type, string expectedItemsCsv)
    {
        var expectedItems = Regex
            .Split(expectedItemsCsv, @"\s*,\s*")
            .Where(i => i.IsPrintable())
            .Select(int.Parse)
            .ToArray();

        var result = CollectionBuilder.BuildCollection(type, expectedItems.Cast<object>());
        Assert.True(type.IsInstanceOfType(result));
        var actual = Assert.IsType<List<int>>(result);
        Assert.Equal(expectedItems, actual);
    }

    [Theory]
    [InlineData(typeof(ReadOnlyCollection<int>), "1,2,3")]
    public void CreateReadOnlyCollection(Type type, string expectedItemsCsv)
    {
        var expectedItems = Regex
            .Split(expectedItemsCsv, @"\s*,\s*")
            .Where(i => i.IsPrintable())
            .Select(int.Parse)
            .ToArray();

        var result = CollectionBuilder.BuildCollection(type, expectedItems.Cast<object>());
        Assert.True(type.IsInstanceOfType(result));
        var actual = Assert.IsType<ReadOnlyCollection<int>>(result);
        Assert.Equal(expectedItems, actual);
    }

    [Theory]
    [InlineData(typeof(ISet<string>), "1,2,3")]
    [InlineData(typeof(HashSet<string>), "1,2,3")]
    public void CreateSets(Type type, string expectedItemsCsv)
    {
        var expectedItems = Regex
            .Split(expectedItemsCsv, @"\s*,\s*")
            .Where(i => i.IsPrintable())
            .ToArray();
        var comparers = new[]
        {
            StringComparer.OrdinalIgnoreCase,
            StringComparer.Ordinal, 
            StringComparer.InvariantCulture, 
            StringComparer.InvariantCultureIgnoreCase
        };

        foreach (var comparer in comparers)
        {
            var result = CollectionBuilder.BuildCollection(type, expectedItems, comparer);
            Assert.True(type.IsInstanceOfType(result));

            var actual = Assert.IsType<HashSet<string>>(result);
            Assert.Equal(comparer, actual.Comparer);
            Assert.Equal(expectedItems.ToHashSet(comparer), actual);
        }
    }


    [Theory]
    [InlineData(typeof(Queue<string>), "1,2,3")]
    public void CreateQueues(Type type, string expectedItemsCsv)
    {
        var expectedItems = Regex
            .Split(expectedItemsCsv, @"\s*,\s*")
            .Where(i => i.IsPrintable())
            .ToArray();

        var result = CollectionBuilder.BuildCollection(type, expectedItems);
        Assert.True(type.IsInstanceOfType(result));

        var actual = Assert.IsType<Queue<string>>(result);

        Assert.Equal(new Queue<string>(expectedItems), actual);
    }

    [Theory]
    [InlineData(typeof(Stack<string>), "1,2,3")]
    public void CreateStacks(Type type, string expectedItemsCsv)
    {
        var expectedItems = Regex
            .Split(expectedItemsCsv, @"\s*,\s*")
            .Where(i => i.IsPrintable())
            .ToArray();

        var result = CollectionBuilder.BuildCollection(type, expectedItems);
        Assert.True(type.IsInstanceOfType(result));

        var actual = Assert.IsType<Stack<string>>(result);

        Assert.Equal(new Stack<string>(expectedItems), actual);
    }


    [Theory]
    [InlineData(typeof(IDictionary<string, int>))]
    [InlineData(typeof(IDictionary<string, Guid>))]
    [InlineData(typeof(Dictionary<string, int>))]
    [InlineData(typeof(Dictionary<string, Guid>))]
    public void CreateDictionaries(Type dictionaryType)
    {
        var comparers = new[]
        {
            StringComparer.OrdinalIgnoreCase,
            StringComparer.Ordinal,
            StringComparer.InvariantCulture,
            StringComparer.InvariantCultureIgnoreCase
        };

        dynamic result = CollectionBuilder.CreateDictionary(dictionaryType);
        Debug.WriteLine(dictionaryType.Name);
        Debug.WriteLine(result.GetType().Name as string);
        Assert.True(dictionaryType.IsInstanceOfType((object)result));
        foreach (var comparer in comparers)
        {
            result = CollectionBuilder.CreateDictionary(dictionaryType, comparer);
            Assert.True(dictionaryType.IsInstanceOfType(result));
            Assert.Equal(comparer, result.Comparer);
        }
    }


}