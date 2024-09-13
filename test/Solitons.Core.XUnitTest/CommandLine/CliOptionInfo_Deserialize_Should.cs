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

        var cache = IInMemoryCache.Create();
        foreach (var scenario in scenarios)
        {
            var (comparer, dictionaryType) = (scenario.Comparer, Type: scenario.DictionaryType);
            metadata.Setup(m => m.GetValueComparer()).Returns(scenario.Comparer);
            var target = new CliOptionInfo(metadata.Object,"test", null, "Test dictionary", dictionaryType)
            {
                IsRequired = true
            };

            var descriptor = target.TypeDescriptor as CliDictionaryTypeDescriptor;
            Assert.NotNull(descriptor);
            Assert.Equal(typeof(Guid), descriptor.ValueType);
            Assert.Equal("Test dictionary", target.Description);

            var expected = Enumerable
            .Range(0, 5)
                .ToDictionary(i => $"key_{i}", _ => Guid.NewGuid(), comparer);

            var input = expected
                .Select(pair => $"{pair.Key} {pair.Value}")
                .Join(",");
            Debug.WriteLine($"Input: {input}");

            var match = Regex.Match(input, $@"(?xis-m)(?: (?<{target.RegexMatchGroupName}>\w+\s+[^\s,]+) | .?)*");
            var actual = (Dictionary<string, Guid>)target.Deserialize(match, key => key)!;

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
        var target = new CliOptionInfo(metadata.Object,"test", null, "Test collection", collectionType)
        {
            IsRequired = true
        };

        var descriptor = target.TypeDescriptor as CliCollectionOptionTypeDescriptor;
        Assert.NotNull(descriptor);
        Assert.Equal(expectedItemType, descriptor.ItemType);
        Assert.Equal("Test collection", target.Description);

        var converter = TypeDescriptor.GetConverter(expectedItemType);
        var expected = Regex
            .Split(itemsCsv, @"[\s,]+")
            .Select(item => converter.ConvertFromInvariantString(item))
            .ToArray();

        var input = itemsCsv;
        Debug.WriteLine($"Input: {input}");

        var match = Regex.Match(input, $@"(?xis-m)(?<{target.RegexMatchGroupName}>\S+)");
        var actual = target.Deserialize(match, key => key);

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

        var target = new CliOptionInfo(metadata.Object,"test", null, "Test collection", collectionType)
        {
            IsRequired = true
        };

        var descriptor = target.TypeDescriptor as CliCollectionOptionTypeDescriptor;
        Assert.NotNull(descriptor);
        Assert.Equal(expectedItemType, descriptor.ItemType);
        Assert.Equal("Test collection", target.Description);

        var converter = TypeDescriptor.GetConverter(expectedItemType);
        var expected = Regex
            .Split(itemsCsv, @"[\s,]+")
            .Select(item => converter.ConvertFromInvariantString(item))
            .Cast<object>()
            .ToHashSet();

        var input = itemsCsv;
        Debug.WriteLine($"Input: {input}");

        var match = Regex.Match(input, $@"(?xis-m)(?<{target.RegexMatchGroupName}>\S+)");
        var actual = target.Deserialize(match, key => key);

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

            var target = new CliOptionInfo(metadata.Object,"test", null, "Test collection", collectionType)
            {
                IsRequired = true
            };

            var descriptor = target.TypeDescriptor as CliCollectionOptionTypeDescriptor;
            Assert.NotNull(descriptor);
            Assert.Equal(expectedItemType, descriptor.ItemType);
            Assert.Equal("Test collection", target.Description);

            var expected = Regex
                .Split(itemsCsv, @"[\s,]+")
                .ToHashSet(comparer);

            var input = itemsCsv;
            Debug.WriteLine($"Input: {input}");

            var match = Regex.Match(input, $@"(?xis-m)(?<{target.RegexMatchGroupName}>\S+)");
            var actual = target.Deserialize(match, key => key);

            Assert.True(collectionType.IsInstanceOfType(actual));

            var actualSet = ((IEnumerable)actual).Cast<object>().ToHashSet();
            Assert.True(actualSet.All(item => expected.Contains(item)));
            Assert.True(expected.All(item => actualSet.Contains(item)));
        }

    }

    [Theory]
    [InlineData("", "Invalid input for option '--map'. Expected a key-value pair but received ''. Please provide both a key and a value.")]
    [InlineData("key1 ", "A value is missing for the key 'key1' in option '--map'. Please specify a corresponding value.")]
    [InlineData("[key2] ", "A value is missing for the key 'key2' in option '--map'. Please specify a corresponding value.")]
    [InlineData("[key3] -other", "A value is missing for the key 'key3' in option '--map'. Please specify a corresponding value.")]
    public void ThrowCliExitExceptionOnIncompleteDictionaryCapture(
        string input,
        string expectedMessage)
    {
        var metadata = new Mock<ICliOptionMetadata>();
        metadata.SetupGet(m => m.Aliases).Returns(new[] { "--map" });
        var target = new CliOptionInfo(
            metadata.Object,
            "test",
            null, 
            "Test dictionary", 
            typeof(Dictionary<string, string>))
        {
            IsRequired = true
        };

        var descriptor = target.TypeDescriptor as CliDictionaryTypeDescriptor;
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(string), descriptor.ValueType);
        Assert.Equal("Test dictionary", target.Description);

        //var inputs = FluentArray.Create("", "key ", "[key] ");
        Debug.WriteLine($"Input: {input}");
        Debug.WriteLine($"Expected message: {input}");

        var match = Regex.Match(input, $@"(?xis-m)(?<{target.RegexMatchGroupName}>.*)");
        var exception = Assert.Throws<CliExitException>(() => target.Deserialize(match, key => key));
        Debug.WriteLine($"Actual message: {exception.Message}");
        Assert.Equal(expectedMessage, exception.Message, StringComparer.Ordinal);
    }
}