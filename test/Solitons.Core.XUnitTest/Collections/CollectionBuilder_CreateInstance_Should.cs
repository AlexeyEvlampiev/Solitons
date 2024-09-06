using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        var result = CollectionBuilder.CreateInstance(type, expectedItems.Cast<object>());
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

        var result = CollectionBuilder.CreateInstance(type, expectedItems.Cast<object>());
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

        var result = CollectionBuilder.CreateInstance(type, expectedItems.Cast<object>());
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
        var comparers = new StringComparer[]
        {
            StringComparer.OrdinalIgnoreCase,
            StringComparer.Ordinal, 
            StringComparer.InvariantCulture, 
            StringComparer.InvariantCultureIgnoreCase
        };

        foreach (var comparer in comparers)
        {
            var result = CollectionBuilder.CreateInstance(type, expectedItems, comparer);
            Assert.True(type.IsInstanceOfType(result));

            var actual = Assert.IsType<HashSet<string>>(result);
            Assert.Equal(comparer, actual.Comparer);
            Assert.Equal(expectedItems.ToHashSet(comparer), actual);
        }
        
    }

}