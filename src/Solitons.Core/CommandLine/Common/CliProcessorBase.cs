using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine.Common;

public abstract class CliProcessorBase
{
    [DebuggerStepThrough]
    public int Process(string commandLine)
    {
        var commandLineObject = CliCommandLine.FromArgs(commandLine);
        return SafeProcess(commandLineObject);
    }


    public int Process(params string[] args)
    {
        var commandLineObject = CliCommandLine.FromArgs(args);
        return SafeProcess(commandLineObject);
    }

    [DebuggerStepThrough]
    private int SafeProcess(CliCommandLine commandLine)
    {
        try
        {
            return Process(commandLine);
        }
        catch (CliExitException e)
        {
            if (e.Message.IsPrintable())
            {
                Console.WriteLine(e.Message);
            }
            return e.ExitCode;
        }
        catch (Exception e)
        {
            return 5;
        }
    }

    private int Process(CliCommandLine commandLine)
    {
        if (IsGeneralHelpRequest(commandLine))
        {
            DisplayGeneralHelp(commandLine);
            return 0;
        }

        var actions = Match(commandLine);
        if (actions.Length == 1)
        {
            if (IsActionHelpRequest(commandLine))
            {
                actions[0].ShowHelp(commandLine);
                return 0;
            }
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


    protected virtual bool IsActionHelpRequest(CliCommandLine commandLine)
    {
        var regex = new Regex(@"(?xis)^(?:--help|help|-?h|-?\?|)$");
        if (commandLine.Segments.Any(segment => regex.IsMatch(segment)))
        {
            return true;
        }

        if (commandLine.Options.Any(o => regex.IsMatch(o.Name)))
        {
            return true;
        }

        return false;
    }


    private int Process(CliCommandLine commandLine, CliAction action)
    {
        OnProcessing(commandLine);
        var exitCode = action.Process(commandLine);
        OnProcessed(commandLine, exitCode);
        return exitCode;
    }


    protected virtual void OnProcessed(CliCommandLine commandLine, int exitCode) { }



    protected virtual void OnProcessing(CliCommandLine commandLine) { }

    protected virtual void OnActionNotFound(CliCommandLine commandLine)
    {
        PrintError("Not found");
        DisplayGeneralHelp(commandLine);
    }

    protected virtual void PrintError(string message) => Console.Error.WriteLine(message);

    protected abstract void DisplayGeneralHelp(CliCommandLine commandLine);

    protected virtual bool IsGeneralHelpRequest(CliCommandLine commandLine)
    {
        if (commandLine.Segments.Length != 1)
        {
            return false;
        }

        var segment = commandLine.Segments[0];
        return Regex.IsMatch(segment, @"(?xis)^(?:--help|help|-?h|-?\?|)$");
    }


    protected abstract IEnumerable<CliAction> GetActions();

    protected virtual CliAction[] Match(CliCommandLine commandLine)
    {
        var matches = GetActions()
            .Where(a => a.IsMatch(commandLine))
            .ToList();

        if (!matches.Any())
        {
            return [];
        }

        var groupedByRank = matches
            .GroupBy(a => Math.Round(a.RankByOptions(commandLine), 3))
            .OrderByDescending(topMatches => topMatches.Key)
            .ToList();

        var selected = groupedByRank
            .Take(1)
            .SelectMany(all => all)
            .ToArray();

        return selected;

    }

    protected abstract class CliAction
    {
        public abstract bool IsMatch(CliCommandLine commandLine);
        public abstract double Rank(CliCommandLine commandLine);
        public abstract int Process(CliCommandLine commandLine);
        public abstract void ShowHelp(CliCommandLine commandLine);

        public abstract double RankByOptions(CliCommandLine commandLine);
    }
}