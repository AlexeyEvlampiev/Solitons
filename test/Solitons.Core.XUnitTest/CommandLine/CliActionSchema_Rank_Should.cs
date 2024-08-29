using System.Diagnostics;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliActionSchema_Rank_Should
{
    [Theory]
    [InlineData("program run arg",2 + 1 /* optimal match */ )]
    [InlineData("program arg run",1)]
    [InlineData("program run run", 1)]
    [InlineData("program arg arg", 0)]
    [InlineData("program", 0)]
    [InlineData("program --hello", 0)]
    [InlineData("program --hello --world", 0)]
    public void HandleScenario001(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand(["run"])
            .AddArgument("arg");

        var actualRank = schema.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }



    [Theory]
    [InlineData("program run arg1 arg2", 3 + 1 /* optimal match */ )]
    [InlineData("program arg1 arg2 run", 2)]
    [InlineData("program run run", 1)]
    [InlineData("program arg1 arg2", 1)]
    [InlineData("program", 0)]
    [InlineData("program --hello", 0)]
    [InlineData("program --hello --world", 0)]
    public void HandleScenario002(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand(["run"])
            .AddArgument("arg1")
            .AddArgument("arg2");

        var actualRank = schema.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }



    [Theory]
    [InlineData("program task 1 run", 3 + 1 /* optimal match */ )]
    [InlineData("program 1 2 task run", 2)]
    [InlineData("program task run", 2)]
    [InlineData("program task task", 1)]
    [InlineData("program run run", 1)]
    [InlineData("program 1 2 3", 0)]
    [InlineData("program", 0)]
    [InlineData("program --hello", 0)]
    [InlineData("program --hello --world", 0)]
    public void HandleScenario003(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand(["task"])
            .AddArgument("number")
            .AddSubCommand(["run"]);

        var actualRank = schema.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [Theory]
    [InlineData("program task 1 run --task-priority 100 --async", 5 + 1 /* optimal match */ )]
    [InlineData("program task 1 run -priority 100 --async", 5 + 1 /* optimal match */ )]
    public void HandleScenario004(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand(["task"])
            .AddArgument("taskNumber")
            .AddSubCommand(["run"])
            .AddScalarOption("priority", ["--task-priority", "-priority", "-p"])
            .AddFlagOption("async", ["--async"]);

        var actualRank = schema.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }


    [Theory]
    [InlineData("program run --parameter[key] value", 2 + 1 /* optimal match */ )]
    [InlineData("program run --parameter.key value", 2 + 1 /* optimal match */ )]
    [InlineData("program run --parameter.key1 value1 --parameter.key2 value2", 2 + 1 /* optimal match */ )]
    [InlineData("program run --parameter[key1] value1 --parameter[key2] value2", 2 + 1 /* optimal match */ )]
    public void HandleScenario005(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand(["run"])
            .AddMapOption("parameters", ["--parameter", "-param", "-p"]);

        var actualRank = schema.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [Theory]
    [InlineData("program --parameter[key] value", 1 + 1 /* optimal match */ )]
    [InlineData("program --parameter.key value", 1 + 1 /* optimal match */ )]
    [InlineData("program --parameter.key1 value1 --parameter.key2 value2", 1 + 1 /* optimal match */ )]
    [InlineData("program --parameter[key1] value1 --parameter[key2] value2", 1 + 1 /* optimal match */ )]
    public void HandleScenario006(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema()
            .AddSubCommand([""])
            .AddMapOption("parameters", ["--parameter", "-param", "-p"]);

        var actualRank = schema.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }
}