using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Solitons.Caching;

namespace Solitons.CommandLine;

public interface ICliProcessor
{

    public sealed int Process2(string commandLine)
    {
        var args = CliCommandLine.FromArgs(commandLine);

        throw new NotImplementedException();
    }

    public sealed int Process(string commandLine)
    {
        try
        {
            



            var context = new CliContext(commandLine);
            commandLine = context.EncodedCommandLine;
            var decoder = context.Decoder;

            if (context.IsCommandListRequest)
            {
                ShowCommandList(context.ProgramName);
                return 0;
            }

            if (IsCommandHelpRequest(commandLine))
            {
                ShowCommandHelp(commandLine, decoder);
                return 0;
            }

            var actions = FindActions(commandLine);

            if (actions.Count == 1)
            {
                return Invoke(actions[0], commandLine, decoder);
            }

            if (actions.Count > 0)
            {
                OnMultipleMatchesFound();
                ShowCommandHelp(commandLine, decoder);
                return 1;
            }

            Debug.Assert(actions.Count == 0);

            if (context.IsEmpty)
            {
                ShowCommandList(context.ProgramName);
                return 1;
            }

            OnNoMatch();
            ShowCommandHelp(commandLine, decoder);

            return 1;
        }
        catch (CliExitException e)
        {
            ShowExitMessage(e);
            return e.ExitCode;
        }
        catch (Exception e)
        {
            OnInternalError(e);
            return 1;
        }
    }

    void ShowCommandList(string programName);


    [DebuggerStepThrough]
    public sealed int Process() => Process(Environment.CommandLine);

    [DebuggerStepThrough]
    public static ICliProcessor Setup(Action<ICliConfigOptions> config) => new CliProcessor(config);


    internal int Invoke(ICliAction action, string commandLine, CliTokenDecoder decoder)
    {
        var cache = IMemoryCache.Create();
        var result = action.Execute(commandLine, decoder, cache);
        return result;
    }

    void OnInternalError(Exception e)
    {
        Trace.TraceError(e.ToString());
        Console.WriteLine("Internal error");
    }

    void ShowExitMessage(CliExitException e)
    {
        if (e.ExitCode != 0)
        {
            Console.Error.WriteLine(e.Message);
        }
        else
        {
            Console.WriteLine(e.Message);
        }
    }


    void OnMultipleMatchesFound();

    void OnNoMatch();


    internal ICliAction[] GetActions();

    internal sealed IReadOnlyList<ICliAction> FindActions(string commandLine)
    {
        return GetActions()
            .Where(a => a.IsMatch(commandLine))
            .GroupBy(a => a.Rank(commandLine))
            .OrderByDescending(group => group.Key)
            .Do(group => Trace.WriteLine(group.Count()))
            .Take(1)
            .SelectMany(similarMatchedActions => similarMatchedActions)
            .ToList();
    }

    internal void ShowCommandHelp(string commandLine, CliTokenDecoder decoder);


    protected sealed bool IsCommandHelpRequest(string commandLine) => CliHelpOptionAttribute.IsMatch(commandLine);

}