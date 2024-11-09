using System;
using System.Linq;
using Xunit;

namespace Solitons.CommandLine;

public sealed class ABCommandLine_Parse_Should
{
    [Fact]
    public void Work()
    {
        var cl = ABCommandLine.ParseCommandLine(
            "tool cmd1 cmd2 --opt1 --opt2 item --opt3 item1 item2 --opt4.key value --opt5.key value1 value2 --opt6[key] value --opt7.some_flag");
        Assert.Equal("tool", cl.ApplicationName);

        Assert.Equal(7, cl.ParsedOptions.Length);
        Assert.True(cl.ParsedOptions[0] is CliFlagOptionCapture { Name: "--opt1" });

        Assert.True(cl.ParsedOptions[1] is CliScalarOptionCapture { Name: "--opt2", Value: "item" });

        Assert.True(cl.ParsedOptions[2] is CliCollectionOptionCapture { Name: "--opt3" } co
                    && co.Values.SequenceEqual(["item1", "item2"], StringComparer.Ordinal));

        Assert.True(cl.ParsedOptions[3] is CliKeyValueOptionCapture { Name: "--opt4", Key: "key", Value: "value" });

        Assert.True(cl.ParsedOptions[4] is CliKeyCollectionOptionCapture { Name: "--opt5", Key: "key" } kco
                    && kco.Values.SequenceEqual(["value1", "value2"], StringComparer.Ordinal));

        Assert.True(cl.ParsedOptions[5] is CliKeyValueOptionCapture { Name: "--opt6", Key: "key", Value: "value" });

        Assert.True(cl.ParsedOptions[6] is CliKeyFlagOptionCapture { Name: "--opt7", Key: "some_flag" });
    }
}