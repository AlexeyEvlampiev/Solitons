using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Moq;
using Solitons.Collections;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliOptionInfo_Deserialize_Should
{
    [Fact]
    public void CreateDictionaries()
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

        foreach (var scenario in scenarios)
        {
            var (comparer, dictionaryType) = (scenario.Comparer, Type: scenario.DictionaryType);
            metadata.Setup(m => m.GetDictionaryKeyComparer()).Returns(scenario.Comparer);
            var target = new CliOptionInfo(metadata.Object, null, "Test dictionary", dictionaryType)
            {
                IsRequired = true
            };

            var descriptor = target.TypeDescriptor as CliDictionaryTypeDescriptor;
            Assert.NotNull(descriptor);
            Assert.Equal(typeof(Guid), descriptor.ValueType);
            Assert.True(descriptor.AcceptsCustomStringComparer);
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

        var target = new CliOptionInfo(metadata.Object, null, "Test dictionary", typeof(Dictionary<string, string>))
        {
            IsRequired = true
        };

        var descriptor = target.TypeDescriptor as CliDictionaryTypeDescriptor;
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(string), descriptor.ValueType);
        Assert.True(descriptor.AcceptsCustomStringComparer);
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