using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Moq;
using Solitons.Caching;
using Solitons.Collections;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliOptionInfo_Deserialize_Should
{
    [Fact]
    public void CreateDictionaries_WithCustomComparers()
    {
        var scenarios =
            from comparer in typeof(StringComparer)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Select(p => p.GetValue(null))
                .OfType<StringComparer>()
            from type in FluentArray.Create(
                typeof(IDictionary<string, Guid>), 
                typeof(Dictionary<string, Guid>))
            select new
            {
                Comparer = comparer,
                DictionaryType = type
            };

        var metadata = new Mock<ICliOptionMetadata>();
        metadata.SetupGet(m => m.Aliases).Returns(new[] { "--test" });
        metadata.Setup(m => m.CanAccept(typeof(Guid), out It.Ref<TypeConverter>.IsAny))
            .Returns((Type _, out TypeConverter converter) =>
            {
                converter = new GuidConverter(); // Set the out parameter
                return true; // Assuming CanAccept returns true
            });

        var cache = IInMemoryCache.Create();
        foreach (var scenario in scenarios)
        {
            var (comparer, dictionaryType) = (scenario.Comparer, Type: scenario.DictionaryType);
            metadata.Setup(m => m.GetValueComparer()).Returns(scenario.Comparer);
            var dictionary = CliOptionInfo.Create(
                metadata.Object, 
                "test", 
                null, 
                "Test dictionary", 
                dictionaryType, 
                true) as CliDictionaryOptionInfo;

            Assert.NotNull(dictionary);
            Assert.Equal(typeof(Guid), dictionary.ValueType);
            Assert.Equal("Test dictionary", dictionary.Description);

            var expected = Enumerable
            .Range(0, 5)
                .ToDictionary(i => $"key_{i}", _ => Guid.NewGuid(), comparer);

            var input = expected
                .Select(pair => $"{pair.Key} {pair.Value}")
                .Join(",");
            Debug.WriteLine($"Input: {input}");

            var match = Regex.Match(input, $@"(?xis-m)(?: (?<{dictionary.RegexMatchGroupName}>\w+\s+[^\s,]+) | .?)*");
            var actual = (Dictionary<string, Guid>)dictionary.Materialize(match, key => key)!;

            Assert.Equal(expected.Comparer, actual.Comparer);
            Assert.Equal(expected, actual);
        }

  
    }

    [Theory]
    [InlineData(typeof(IEnumerable<int>), typeof(int), "1,2,3,4,5")]
    [InlineData(typeof(IList<int>), typeof(int), "1,2,3")]
    [InlineData(typeof(List<int>), typeof(int), "3,2,1")]
    [InlineData(typeof(IReadOnlyList<int>), typeof(int), "3,2,1")]
    [InlineData(typeof(IReadOnlyCollection<int>), typeof(int), "9,3,2,1")]
    [InlineData(typeof(ICollection<int>), typeof(int), "9,3,2,1")]
    [InlineData(typeof(IEnumerable<Guid>), typeof(Guid), "1b5aa176-5f0c-4f3b-8400-19b39ae797b4,4778b27b-831b-4f87-a6ee-3085c55f9a52")]
    public void CreateCollections_FromCsv(
        Type collectionType, 
        Type expectedItemType,
        string itemsCsv)
    {
        var metadata = new Mock<ICliOptionMetadata>();
        metadata.SetupGet(m => m.Aliases).Returns(new[] { "--test" });
        metadata.SetupGet(m => m.AllowsCsv).Returns(true);
        metadata.Setup(m => m.CanAccept(It.IsAny<Type>(), out It.Ref<TypeConverter>.IsAny))
            .Returns((Type type, out TypeConverter converter) =>
            {
                converter = TypeDescriptor.GetConverter(type);
                return true;
            });
        var collection = CliOptionInfo
            .Create(
                metadata.Object, 
                "test", 
                null, 
                "Test collection", 
                collectionType, 
                true) as CliCollectionOptionInfo;

        Assert.NotNull(collection);
        Assert.Equal(expectedItemType, collection.ElementType);
        Assert.Equal("Test collection", collection.Description);

        var converter = TypeDescriptor.GetConverter(expectedItemType);
        var expected = Regex
            .Split(itemsCsv, @"[\s,]+")
            .Select(item => converter.ConvertFromInvariantString(item))
            .ToArray();

        var input = itemsCsv;
        Debug.WriteLine($"Input: {input}");

        var match = Regex.Match(input, $@"(?xis-m)(?<{collection.RegexMatchGroupName}>\S+)");
        var actual = collection.Materialize(match, key => key);

        Assert.True(collectionType.IsInstanceOfType(actual));
        var expectedEnumerator = expected.GetEnumerator();
        var actualEnumerator = ((IEnumerable)actual).GetEnumerator();
        using var disposable = actualEnumerator as IDisposable;

        while (expectedEnumerator.MoveNext())
        {
            Assert.True(actualEnumerator.MoveNext());
            Assert.Equal(expectedEnumerator.Current, actualEnumerator.Current);
        }
    }



    [Theory]
    [InlineData(typeof(HashSet<int>), typeof(int), "1,2,3,4,5")]
    [InlineData(typeof(ISet<int>), typeof(int), "101, 102")]
    [InlineData(typeof(IReadOnlySet<int>), typeof(int), "101, 102")]
    public void CreateSets_FromCsv(
    Type collectionType,
    Type expectedItemType,
    string itemsCsv)
    {
        itemsCsv = Regex.Replace(itemsCsv, @"\s+", string.Empty);
        var metadata = new Mock<ICliOptionMetadata>();
        metadata.SetupGet(m => m.Aliases).Returns(new[] { "--test" });
        metadata.SetupGet(m => m.AllowsCsv).Returns(true);
        metadata.Setup(m => m.CanAccept(It.IsAny<Type>(), out It.Ref<TypeConverter>.IsAny))
            .Returns((Type type, out TypeConverter converter) =>
            {
                converter = TypeDescriptor.GetConverter(type);
                return true;
            });

        var collection = CliOptionInfo
            .Create(metadata.Object, "test", null, "Test collection", collectionType, true) as CliCollectionOptionInfo;

        Assert.NotNull(collection);
        Assert.Equal(expectedItemType, collection.ElementType);
        Assert.Equal("Test collection", collection.Description);

        var converter = TypeDescriptor.GetConverter(expectedItemType);
        var expected = Regex
            .Split(itemsCsv, @"[\s,]+")
            .Select(item => converter.ConvertFromInvariantString(item))
            .Cast<object>()
            .ToHashSet();

        var input = itemsCsv;
        Debug.WriteLine($"Input: {input}");

        var match = Regex.Match(input, $@"(?xis-m)(?<{collection.RegexMatchGroupName}>\S+)");
        var actual = collection.Materialize(match, key => key);

        Assert.True(collectionType.IsInstanceOfType(actual));

        var actualSet = ((IEnumerable)actual).Cast<object>().ToHashSet();
        Assert.True(actualSet.All(item => expected.Contains(item)));
        Assert.True(expected.All(item => actualSet.Contains(item)));
    }

    [Theory]
    [InlineData(typeof(HashSet<string>), typeof(string), "hello, world, Hello, World")]
    public void CreateSets_WithCustomComparers(
        Type collectionType,
        Type expectedItemType,
        string itemsCsv)
    {
        itemsCsv = Regex.Replace(itemsCsv, @"\s+", string.Empty);
        var comparers = typeof(StringComparer)
            .GetProperties(BindingFlags.Static | BindingFlags.Public)
            .Select(p => p.GetValue(null))
            .OfType<StringComparer>();

        var cache = IInMemoryCache.Create();
        foreach (var comparer in comparers)
        {
            var metadata = new Mock<ICliOptionMetadata>();
            metadata.SetupGet(m => m.Aliases).Returns(new[] { "--test" });
            metadata.SetupGet(m => m.AllowsCsv).Returns(true);
            metadata.Setup(m => m.GetValueComparer()).Returns(comparer);
            metadata.Setup(m => m.CanAccept(It.IsAny<Type>(), out It.Ref<TypeConverter>.IsAny))
                .Returns((Type type, out TypeConverter converter) =>
                {
                    converter = TypeDescriptor.GetConverter(type);
                    return true;
                });

            var collection = CliOptionInfo.Create(metadata.Object, "test", null, "Test collection", collectionType, true) as CliCollectionOptionInfo;

            Assert.NotNull(collection);
            Assert.Equal(expectedItemType, collection.ElementType);
            Assert.Equal("Test collection", collection.Description);

            var expected = Regex
                .Split(itemsCsv, @"[\s,]+")
                .ToHashSet(comparer);

            var input = itemsCsv;
            Debug.WriteLine($"Input: {input}");

            var match = Regex.Match(input, $@"(?xis-m)(?<{collection.RegexMatchGroupName}>\S+)");
            var actual = collection.Materialize(match, key => key);

            Assert.True(collectionType.IsInstanceOfType(actual));

            var actualSet = ((IEnumerable)actual).Cast<object>().ToHashSet();
            Assert.True(actualSet.All(item => expected.Contains(item)));
            Assert.True(expected.All(item => actualSet.Contains(item)));
        }

    }

    [Theory]
    [InlineData("")]
    [InlineData("key1 ")]
    [InlineData("[key2] ")]
    [InlineData("[key3] -other")]
    public void ThrowCliExitExceptionOnIncompleteDictionaryCapture(
        string input)
    {
        var metadata = new Mock<ICliOptionMetadata>();
        metadata.SetupGet(m => m.Aliases).Returns(new[] { "--map" });
        metadata.Setup(m => m.CanAccept(It.IsAny<Type>(), out It.Ref<TypeConverter>.IsAny))
            .Returns((Type type, out TypeConverter converter) =>
            {
                converter = TypeDescriptor.GetConverter(type);
                return true;
            });
        var dictionary = CliOptionInfo
            .Create(
            metadata.Object,
            "test",
            null,
            "Test dictionary",
            typeof(Dictionary<string, string>),
            true) as CliDictionaryOptionInfo;

        Assert.NotNull(dictionary);
        Assert.Equal(typeof(string), dictionary.ValueType);
        Assert.Equal("Test dictionary", dictionary.Description);

        //var inputs = FluentArray.Create("", "key ", "[key] ");
        Debug.WriteLine($"Input: {input}");
        Debug.WriteLine($"Expected message: {input}");

        var match = Regex.Match(input, $@"(?xis-m)(?<{dictionary.RegexMatchGroupName}>.*)");
        var exception = Assert.Throws<Exception>(() => dictionary.Materialize(match, key => key));
        Debug.WriteLine($"Actual message: {exception.Message}");
    }
}