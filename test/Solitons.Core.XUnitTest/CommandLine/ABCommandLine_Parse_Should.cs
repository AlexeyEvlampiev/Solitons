using Xunit;

namespace Solitons.CommandLine;

public sealed class ABCommandLine_Parse_Should
{
    [Fact]
    public void Work()
    {
        var cl = ABCommandLine.Parse(
            "tool cmd1 cmd2 --opt1 --opt2 item --opt3 item1 item2 --opt4.key value --opt5.key value1 value2 --opt6[key] value");
        Assert.Equal("tool", cl.ProgramName);

        Assert.Equal(6, cl.OptionsCount);
        Assert.True(cl.IsFlagOption(0, out var name) && name == "--opt1");
        Assert.False(cl.IsFlagOption(1, out _));

    }
}