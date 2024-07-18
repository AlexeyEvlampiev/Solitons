using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine;

public abstract class CommandLineInterface
{
    public interface IOptions
    {
        IOptions Include<T>(BindingFlags binding = BindingFlags.Static | BindingFlags.Public);
    }


    public static CommandLineInterface Build(Action<IOptions> config)
    {
        var options = new CliConfigurations();
        config.Invoke(options);
        return new Cli(options);
    }

    [DebuggerStepThrough]
    public int Execute() => Execute(Environment.CommandLine);

    public int Execute(string commandLine)
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
                PrintHelp(commandLine, actions);
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
            PrintHelp(commandLine, actions);
            return 1;
        }
        catch (Exception e)
        {
            Trace.TraceError(e.ToString());
            Console.WriteLine("Internal error");
            return 1;
        }

    }

    protected abstract IAction[] LoadActions();

    protected abstract bool IsHelpRequestException(Exception ex);


    public interface IAction
    {
        bool IsMatch(string commandLine);
        int Execute(string commandLine);
        Similarity CalcSimilarity(string commandLine);
        void PrintHelp();
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
    



    protected void PrintHelp(string commandLine, IAction[] actions)
    {
        var matchesFound = actions.Any(a => a.IsMatch(commandLine));
        actions
            .Where(a => false == matchesFound || a.IsMatch(commandLine))
            .GroupBy(a => a.CalcSimilarity(commandLine))
            .OrderByDescending(similarActions => similarActions.Key)
            .Take(1)
            .SelectMany(similarActions => similarActions)
            .ForEach((action, index) =>
            {
                if (index > 0)
                {
                    Console.WriteLine();
                }

                action.PrintHelp();
            });
    }
}