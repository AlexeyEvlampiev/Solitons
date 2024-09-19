using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Caching;

namespace Solitons.CommandLine;

public interface ICliProcessor
{
    public sealed int Process(string commandLine)
    {
        try
        {
            commandLine = Encode(commandLine, out var decoder);
            string programName = ExtractProgramName(commandLine, decoder);

            if (IsGeneralHelpRequest(commandLine))
            {
                ShowGeneralHelp(programName);
                return 0;
            }

            if (IsSpecificHelpRequest(commandLine))
            {
                ShowHelpFor(commandLine, decoder);
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
            }
            else
            {
                OnNoMatch();
            }

            ShowHelpFor(commandLine, decoder);

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

    internal string ExtractProgramName(string commandLine, CliTokenDecoder decoder)
    {
        var match = Regex.Match(commandLine, @"^\s*(\S+)");
        Debug.Assert(match.Success);
        Debug.Assert(match.Groups[1].Success);
        var programName = match.Groups[1].Value;
        return decoder(programName);
    }

    internal int Invoke(ICliAction action, string commandLine, CliTokenDecoder decoder)
    {
        var cache = IInMemoryCache.Create();
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

    [DebuggerStepThrough]
    public sealed int Process() => Process(Environment.CommandLine);

    [DebuggerStepThrough]
    public static ICliProcessor Setup(Action<Options> config)
    {
        var processor = new CliProcessor();
        config.Invoke(processor);
        return processor;
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

    internal void ShowHelpFor(string commandLine, CliTokenDecoder decoder);

    void ShowGeneralHelp(string programName);

    bool IsGeneralHelpRequest(string commandLine);

    protected bool IsSpecificHelpRequest(string commandLine);


    internal sealed string Encode(string commandLine, out CliTokenDecoder decoder)
    {
        commandLine = CliTokenEncoder
            .Encode(commandLine, out decoder)
            .Trim();
        return 
            NormalizeProgramName(commandLine, decoder)
            .Trim();
    }

    private string NormalizeProgramName(string commandLine, CliTokenDecoder decoder)
    {
        return Regex.Replace(
            commandLine,
            @"(?xis-m)^\S+",
            match => GetFileName(decoder(match.Value)));
    }

    protected string GetFileName(string filePath);

    public abstract class Options
    {
        public abstract Options UseCommandsFrom(
            object program, 
            string baseRoute = "", 
            BindingFlags binding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public abstract Options UseCommandsFrom(
            Type declaringType,
            string baseRoute = "",
            BindingFlags binding = BindingFlags.Static | BindingFlags.Public);

        public abstract Options UseLogo(string logo);

        public abstract Options UseDescription(string description);

        [DebuggerStepThrough]
        public Options UseCommandsFrom<T>(
            string baseRoute = "",
            BindingFlags binding = BindingFlags.Static | BindingFlags.Public) =>
            UseCommandsFrom(typeof(T), baseRoute, binding);



    }
}