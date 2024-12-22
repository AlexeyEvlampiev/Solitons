using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solitons.CommandLine.Mercury;

public abstract class CliProcessorBase
{
    [DebuggerStepThrough]
    public int Process(string commandLine)
    {
        var commandLineObject = CliCommandLine.FromArgs(commandLine);
        return Process(commandLineObject);
    }


    public int Process(params string[] args)
    {
        throw new NotImplementedException();
    }

    [DebuggerStepThrough]
    private int SafeProcess(CliCommandLine commandLine)
    {
        try
        {
            return Process(commandLine);
        }
        catch (Exception e)
        {
            return 5;
        }
    }

    private int Process(CliCommandLine commandLine)
    {
        if (IsHelpRequest(commandLine))
        {
            ShowHelp(commandLine);
            return 0;
        }

        var actions = Match(commandLine);
        if (actions.Length == 1)
        {
            return Process(commandLine, actions[0]);
        }

        if (actions.Length == 0)
        {
            OnActionNotFound(commandLine);
            return 4;
        }

        Debug.Assert(actions.Length > 1);

        throw new NotImplementedException();
    }


    private int Process(CliCommandLine commandLine, CliAction action)
    {
        OnProcessing(commandLine);
        var exitCode = action.Process(commandLine);
        OnProcessed(commandLine, exitCode);
        return exitCode;
    }


    protected abstract void OnProcessed(CliCommandLine commandLine, int exitCode);



    protected abstract void OnProcessing(CliCommandLine commandLine);

    protected virtual void OnActionNotFound(CliCommandLine commandLine)
    {
        PrintError("Not found");
        ShowHelp(commandLine);
    }

    protected virtual void PrintError(string message) => Console.Error.WriteLine(message);

    protected abstract void ShowHelp(CliCommandLine commandLine);

    protected abstract bool IsHelpRequest(CliCommandLine commandLine);


    protected abstract IEnumerable<CliAction> GetActions();

    protected virtual CliAction[] Match(CliCommandLine commandLine)
    {
        return GetActions()
        .Where(a => a.IsMatch(commandLine))
            .GroupBy(a => Math.Round(a.Rank(commandLine), 3))
            .OrderByDescending(topMatches => topMatches.Key)
            .Take(1)
            .SelectMany(topMatches => topMatches)
            .ToArray();
    }

    protected abstract class CliAction
    {
        public abstract bool IsMatch(CliCommandLine commandLine);
        public abstract double Rank(CliCommandLine commandLine);
        public abstract int Process(CliCommandLine commandLine);
    }
}