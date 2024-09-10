﻿using System.Diagnostics;
using Moq;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliAction_Match_Should
{
    [Theory]
    [InlineData("program run arg",true, 0 )]
    [InlineData("program run arg hello", true, 1)]
    [InlineData("program run arg hello world", true, 2)]
    [InlineData("program run arg --hello --world", true, 2)]
    [InlineData("program arg run", false, 0)]
    [InlineData("program run run", false, 0)]
    [InlineData("program arg arg", false, 0)]
    [InlineData("program", false,0)]
    [InlineData("program --hello", false,0)]
    [InlineData("program --hello --world",false, 0)]
    public void HandleScenario001(string commandLine, bool success, int unrecognizedTokensCount)
    {
        Debug.WriteLine(commandLine);
        var action = CliAction.Create(null, GetType().GetMethod(nameof(ProgramRun))!, [], [] );


        var match = action.Match(
            commandLine, 
            (token) => token, 
            unmatched =>
        {
            Assert.Equal(unrecognizedTokensCount, unmatched.Count);
        });
        Assert.Equal(success, match.Success);
        if (success)
        {
            Assert.True(match.Groups["arg"].Success);
            Assert.Equal(1, match.Groups["arg"].Captures.Count);
            Assert.Equal("arg", match.Groups["arg"].Value);
        }
    }

    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int ProgramRun(string arg) => 0;


    [Theory]
    [InlineData("program task 123 run --timeout 00:30:00 --parameter.server localhost -p[post] 4567", true, 0)]
    [InlineData("program tsk 123 go -to 00:30:00 -p.server localhost --parameters[post] 4567", true, 0)]
    [InlineData("program task 123 run --timeout 00:30:00 --parameter.server localhost -p[post] 4567 --hello", true, 1)]
    [InlineData("program task 123 run --timeout 00:30:00 --parameter.server localhost -p[post] 4567 --hello --world", true, 2)]
    [InlineData("program run 123 task --timeout 00:30:00 --parameter.server localhost -p[post] 4567", false, 0)]
    [InlineData("hello world", false, 0)]
    public void HandleScenario002(string commandLine, bool success, int unrecognizedTokensCount)
    {
        Debug.WriteLine(commandLine);
        var schema = new CliActionSchema(GetType().GetMethod(nameof(Scenario002))!);

        var match = schema.Match(
            commandLine, 
            token => token,
            unmatched =>
        {
            Assert.Equal(unrecognizedTokensCount, unmatched.Count);
        });
        Assert.Equal(success, match.Success);
        if (success)
        {
            Assert.True(match.Groups["taskId"].Success);
            Assert.Equal("123", match.Groups["taskId"].Value);

            Assert.True(match.Groups["timeout"].Success);
            Assert.Equal("00:30:00", match.Groups["timeout"].Value);

            Assert.True(match.Groups["parameters"].Success);
            Assert.Equal(2, match.Groups["parameters"].Captures.Count);

            Assert.Equal("server localhost", match.Groups["parameters"].Captures[0].Value);
            Assert.Equal("[post] 4567", match.Groups["parameters"].Captures[1].Value);
        }
    }


    [CliRoute("run"), CliRouteArgument(nameof(arg), "Description goes here")]
    public int Scenario002(string arg) => 0;
}