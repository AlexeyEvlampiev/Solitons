using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Moq;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliOptionInfo_ctor_Should
{
    [Theory]
    [InlineData(123, "Description goes here", true, 321)]
    [InlineData(null, "Another description", false, 564)]
    [InlineData(null, "Should handle default value", false, null)] // Default value should be used
    [InlineData(999, "Using default value if no match", false, null)] // No match, default should be used
    [InlineData(-1, "Negative values allowed", true, -1)] // Test negative value
    [InlineData(int.MaxValue, "Max int value", true, int.MaxValue)] // Test extreme positive value
    [InlineData(int.MinValue, "Min int value", true, int.MinValue)] // Test extreme negative value
    public void HandleIntValueProperty(int? defaultValue, string description, bool isRequired, int? value)
    {
        var metadata = new Mock<ICliOptionMetadata>();
        metadata.Setup(m => m.CanAccept(It.IsAny<Type>(), out It.Ref<TypeConverter>.IsAny))
            .Returns((Type type, out TypeConverter converter) =>
            {
                converter = TypeDescriptor.GetConverter(type);
                return true; 
            });
        metadata.SetupGet(m => m.Aliases).Returns(new[] { "--alias1", "--alias2", "-short1", "-short2" });
        var target = CliOptionInfo
            .Create(
                metadata.Object,
                "test", 
                defaultValue, 
                description, 
                typeof(int), 
                isRequired);

        Assert.Equal(description, target.Description, StringComparer.Ordinal);
        Assert.Equal(isRequired, target.IsRequired);

        string input = value?.ToString() ?? ""; // Convert value to string, handle null cases
        var match = Regex.Match(input, $@"^(?<{target.RegexMatchGroupName}>\d+)?.*$");
        var group = match.Groups[target.RegexMatchGroupName];
        if (group.Success && value.HasValue)
        {
            Assert.Equal(value, target.Deserialize(match, key => key)); 
        }
        else if (!group.Success && isRequired)
        {
            Assert.Throws<CliExitException>(() => target.Deserialize(match, key => key));
        }
        else
        {
            // If no match and not required, should return the default value
            Assert.Equal(defaultValue, target.Deserialize(match, key => key));
        }
    }
}