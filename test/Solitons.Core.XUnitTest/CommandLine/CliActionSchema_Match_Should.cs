﻿using System;
using System.Diagnostics;
using Solitons.Caching;
using Xunit;

namespace Solitons.CommandLine;

// ReSharper disable once InconsistentNaming
public sealed class CliAction_Match_Should
{
    [Theory]
    [InlineData("program run arg",true)]
    [InlineData("program run arg hello", true)]
    [InlineData("program run arg hello world", true)]
    [InlineData("program run arg --hello --world", true)]
    [InlineData("program arg run", false)]
    [InlineData("program run run", false)]
    [InlineData("program arg arg", false)]
    [InlineData("program", false)]
    [InlineData("program --hello", false)]
    [InlineData("program --hello --world",false)]
    public void HandleScenario001(string commandLine, bool expectedMatchResult)
    {
        Debug.WriteLine(commandLine);
        var cache = IMemoryCache.Create();
        var action = CliActionOld.Create(
            null, 
            GetType()
                .GetMethod(nameof(ProgramRun))!, 
            [], 
            "",
            [],
            cache );

        Assert.Equal(expectedMatchResult, action.IsMatch(commandLine));

        if (false == expectedMatchResult)
        {
            Assert.Throws<InvalidOperationException>(() => action.Execute(commandLine, key => key, cache));
        }
    }

    [CliRoute("run"), CliArgument(nameof(arg), "Description goes here")]
    public int ProgramRun(string arg) => 0;
}