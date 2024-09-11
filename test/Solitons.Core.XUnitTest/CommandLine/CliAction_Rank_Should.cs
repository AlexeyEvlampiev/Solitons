using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliAction_Rank_Should
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
        var action = CliAction.Create(null, GetType().GetMethod(nameof(ProgramRun))!, [], []);

        var actualRank = action.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int ProgramRun(string arg) => 0;

    [Theory]
    [InlineData("program run arg1", 2 + 1 /* optimal match */ )]
    [InlineData("program arg1 arg2 run", 1)]
    [InlineData("program run run", 1)]
    [InlineData("program arg1 arg2", 0)]
    [InlineData("program", 0)]
    [InlineData("program --hello", 0)]
    [InlineData("program --hello --world", 0)]
    public void HandleScenario002(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var action = CliAction.Create(null, GetType().GetMethod(nameof(ProgramRun2))!, [], []);

        var actualRank = action.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int ProgramRun2(string arg) => 0;

    [Theory]
    [InlineData("program task 1 run", 1)]
    [InlineData("program 1 2 task run", 1)]
    [InlineData("program task run", 1)]
    [InlineData("program task task", 0)]
    [InlineData("program run run", 1)]
    [InlineData("program 1 2 3", 0)]
    [InlineData("program", 0)]
    [InlineData("program --hello", 0)]
    [InlineData("program --hello --world", 0)]
    public void HandleScenario003(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var action = CliAction.Create(null, GetType().GetMethod(nameof(TaskRun))!, [], []);

        var actualRank = action.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int TaskRun(string arg) => 0;

    [Theory]
    [InlineData("program task 1 run --task-priority 100 --async", 1)]
    [InlineData("program task 1 run -priority 100 --async", 1)]
    public void HandleScenario004(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var action = CliAction.Create(null, GetType().GetMethod(nameof(TaskRun2))!, [], []);

        var actualRank = action.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int TaskRun2(string arg) => 0;

    [Theory]
    [InlineData("program run --parameter[key] value", 2 )]
    [InlineData("program run --parameter.key value", 2 )]
    [InlineData("program run --parameter.key1 value1 --parameter.key2 value2", 2 )]
    [InlineData("program run --parameter[key1] value1 --parameter[key2] value2", 2 )]
    public void HandleScenario005(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var action = CliAction.Create(null, GetType().GetMethod(nameof(Scenario005))!, [], []);

        var actualRank = action.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int Scenario005(
        string arg,
        [CliOption("--parameter")]Dictionary<string, string> parameters) => 0;

    [Theory]
    [InlineData("program run arg --parameter[key] value", 3 + 1 /* optimal match */ )]
    [InlineData("program run --parameter.key value", 2 )]
    [InlineData("program --parameter.key1 value1 --parameter.key2 value2", 1)]
    [InlineData("program --parameter[key1] value1 --parameter[key2] value2", 1)]
    public void HandleScenario006(string commandLine, int expectedRank)
    {
        Debug.WriteLine(commandLine);
        var action = CliAction.Create(null, GetType().GetMethod(nameof(Scenario006))!, [], []);

        var actualRank = action.Rank(commandLine);
        Assert.Equal(expectedRank, actualRank);
    }

    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int Scenario006(
        string arg,
        [CliOption("--parameter")] Dictionary<string, string> parameters) => 0;
}