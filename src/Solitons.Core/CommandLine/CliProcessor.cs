using Solitons.CommandLine.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

public abstract class CliProcessor
{
    public interface IOptions
    {
        IOptions UseCommandHandlersFrom<T>(BindingFlags binding = BindingFlags.Static | BindingFlags.Public);
        void OnNoArguments(CliHelpMessageHandler handler);
    }


    public static CliProcessor Setup(Action<IOptions> config)
    {
        var options = new CliConfigurations();
        config.Invoke(options);
        return new CliProcessorImpl(options);
    }

    [DebuggerStepThrough]
    public int Process() => Process(Environment.CommandLine);

    public int Process(string commandLine)
    {
        commandLine = commandLine.Trim();
        var actions = LoadActions();
        
        try
        {
            var selectedActions = actions
                .Where(a => a.IsMatch(commandLine))
                .GroupBy(a => a.CalcSimilarity(commandLine))
                .OrderByDescending(similarMatchedActions => similarMatchedActions.Key)
                .Take(1)
                .SelectMany(similarMatchedActions => similarMatchedActions)
                .ToList();

            if (selectedActions.Count != 1)
            {
                Trace.TraceInformation($"Found {selectedActions.Count} actions that matched the given command line.");
                var help = BuildHelpMessage(commandLine, actions);
                var x = TokenSubstitutionPreprocessor.SubstituteTokens(commandLine, out _);
                var empty = Regex.IsMatch(x, @"\s");
                if (empty)
                {
                    OnNoArguments();
                }
                return 1;
            }



            var action = selectedActions[0];

            Trace.TraceInformation($"Found an actions that matches the given command line.");

            var result = action.Execute(commandLine);
            Trace.TraceInformation($"The action returned {result}");

            return result;
        }
        catch (Exception e) when(IsHelpRequestException(e))
        {
            BuildHelpMessage(commandLine, actions);
            return 1;
        }
        catch (Exception e)
        {
            Trace.TraceError(e.ToString());
            Console.WriteLine("Internal error");
            return 1;
        }

    }

    protected abstract void OnNoArguments(string message);

    protected abstract IAction[] LoadActions();

    protected abstract bool IsHelpRequestException(Exception ex);


    public interface IAction
    {
        bool IsMatch(string commandLine);
        int Execute(string commandLine);
        Similarity CalcSimilarity(string commandLine);
        string BuildHelpMessage();
    }



    public abstract class Similarity : IComparable<Similarity>
    {
        protected abstract int CompareTo(Similarity other);

        [DebuggerStepThrough]
        int IComparable<Similarity>.CompareTo(Similarity? other)
        {
            if (other?.GetType() != this.GetType())
            {
                throw new ArgumentOutOfRangeException();
            }

            return CompareTo(other);
        }
    }
    



    protected string BuildHelpMessage(string commandLine, IAction[] actions)
    {
        var matchesFound = actions.Any(a => a.IsMatch(commandLine));
        var help = new StringBuilder();
        actions
            .Where(a => false == matchesFound || a.IsMatch(commandLine))
            .GroupBy(a => a.CalcSimilarity(commandLine))
            .OrderByDescending(similarActions => similarActions.Key)
            .Take(1)
            .SelectMany(similarActions => similarActions)
            .ForEach((action) =>
            {
                help.AppendLine(action.BuildHelpMessage());
            });
        return help.ToString();
    }
}