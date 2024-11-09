using System;
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

        Assert.Equal(7, cl.Options.Length);
        Assert.True(cl.Options[0] is CliFlagOptionCapture { Name: "--opt1" });

        Assert.True(cl.Options[1] is CliScalarOptionCapture { Name: "--opt2", Value: "item" });

        Assert.True(cl.Options[2] is CliCollectionOptionCapture { Name: "--opt3" } co
                    && co.Values.SequenceEqual(["item1", "item2"], StringComparer.Ordinal));

        Assert.True(cl.Options[3] is CliKeyValueOptionCapture { Name: "--opt4", Key: "key", Value: "value" });

        Assert.True(cl.Options[4] is CliKeyCollectionOptionCapture { Name: "--opt5", Key: "key" } kco
                    && kco.Values.SequenceEqual(["value1", "value2"], StringComparer.Ordinal));

        Assert.True(cl.Options[5] is CliKeyValueOptionCapture { Name: "--opt6", Key: "key", Value: "value" });

        Assert.True(cl.Options[6] is CliKeyFlagOptionCapture { Name: "--opt7", Key: "some_flag" });
    }
}