using System.Linq;
using Xunit;

namespace Solitons.CommandLine;

public sealed class ABCommandLine_Parse_Should
{
    [Fact]
    public void Work()
    {
        var cl = ABCommandLine.Parse(
            "tool cmd1 cmd2 --opt1 --opt2 item --opt3 item1 item2 --opt4.key value --opt5.key value1 value2 --opt6[key] value --opt7.some_flag");
        Assert.Equal("tool", cl.ExecutableName);

        Assert.Equal(7, cl.OptionCount);
        Assert.True(cl.IsFlagOption(0, out var optionName) && optionName == "--opt1");

        Assert.True(cl.IsScalarOption(1, out optionName, out var optionValue) 
                    && optionName == "--opt2"
                    && optionValue == "item");

        Assert.True(cl.IsCollectionOption(2, out optionName, out var optionValues)
                    && optionName == "--opt3"
                    && optionValues.Length == 2
                    && optionValues.Contains("item1")
                    && optionValues.Contains("item2"));

        Assert.True(cl.IsKeyValueOption(3, out optionName, out var optionKey, out optionValue)
                    && optionName == "--opt4"
                    && optionKey == "key"
                    && optionValue == "value");

        Assert.True(cl.IsKeyCollectionOption(4, out optionName, out optionKey, out optionValues)
                    && optionName == "--opt5"
                    && optionValues.Length == 2
                    && optionValues.Contains("value1")
                    && optionValues.Contains("value2"));

        Assert.True(cl.IsKeyValueOption(5, out optionName, out  optionKey, out optionValue)
                    && optionName == "--opt6"
                    && optionKey == "key"
                    && optionValue == "value");

        Assert.True(cl.IsKeyFlagOption(6, out optionName, out optionKey)
                    && optionName == "--opt7"
                    && optionKey == "some_flag");
    }
}