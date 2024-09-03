using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Func<object?[], Task<int>> _actionHandler;
    private readonly CliMasterOptionBundle[] _masterOptions;
    private readonly ICliActionSchema _schema;
    private readonly ICliCommandMethodParametersFactory _parametersFactory;

    [DebuggerNonUserCode]
    internal CliAction(
        Func<object?[], Task<int>> actionHandler,
        ICliActionSchema schema,
        ICliCommandMethodParametersFactory parametersFactory,
        CliMasterOptionBundle[] masterOptions)
    {
        _actionHandler = actionHandler;
        _schema = schema;
        _parametersFactory = parametersFactory;
        _masterOptions = masterOptions;
    }


    internal static CliAction Create(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptionBundles,
        IEnumerable<CliRouteAttribute> baseRouteMetadata)
    {
        ThrowIf.ArgumentNull(method);
        ThrowIf.ArgumentNull(masterOptionBundles);

        var schema = new CliActionSchema(method, masterOptionBundles, baseRouteMetadata);
        var parametersFactory = new CliActionHandlerParametersFactory(method);
        return new CliAction(InvokeAsync, schema, parametersFactory, masterOptionBundles);

        [DebuggerStepThrough]
        async Task<int> InvokeAsync(object?[] args)
        {
            Debug.WriteLine($"Invoking '{method.Name}'");
            var result = method.Invoke(instance, args);
            Debug.WriteLine($"Invoking '{method.Name}' returned '{result}'");
            if (result is Task task)
            {
                Debug.WriteLine($"Awaiting '{method.Name}' returned task");
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task) ?? 0;
                    Debug.WriteLine($"'{method.Name}' returned task result is '{result}'");
                }
            }

            if (result is int exitCode)
            {
                return exitCode;
            }

            return 0;
        }
    }


    public int Execute(string commandLine, CliTokenSubstitutionPreprocessor preProcessor)
    {
        commandLine = ThrowIf.ArgumentNullOrWhiteSpace(commandLine);
        var match = _schema.Match(
            commandLine, 
            preProcessor, 
            unmatchedTokens =>
        {
            var csv = unmatchedTokens
                .Join(", ");
            CliExit.With(
                $"The following options are not recognized as valid for the command: {csv}. " +
                $"Please check the command syntax.");
        });

        if (match.Success == false)
        {
            throw new InvalidOperationException($"The command line did not match any known patterns.");
        }

        var args = _parametersFactory.BuildMethodArguments(match, preProcessor);

        foreach (var bundle in _masterOptions)
        {
            bundle.PopulateFrom(match, preProcessor);
        }

        _masterOptions.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var task = _actionHandler.Invoke(args);
            task.GetAwaiter().GetResult();
            var resultProperty = task.GetType().GetProperty("Result");
            object result = 0;
            if (resultProperty != null)
            {
                result = resultProperty.GetValue(task) ?? 0;
            }

            _masterOptions.ForEach(bundle => bundle.OnActionExecuted(commandLine));
            
            return result is int exitCode ? exitCode : 0;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.GetType().Name);
            _masterOptions.ForEach(bundle => bundle.OnError(commandLine, e));
            throw;
        }


    }

    [DebuggerStepThrough]
    public int Rank(string commandLine) => _schema.Rank(commandLine);


    [DebuggerStepThrough]
    public bool IsMatch(string commandLine) => _schema.IsMatch(commandLine);

    public void ShowHelp()
    {
        Console.WriteLine(GetHelpText());
    }


    public int CompareTo(CliAction? other)
    {
        other = ThrowIf.ArgumentNull(other, "Cannot compare to a null object.");
        return String.Compare(_schema.CommandRouteExpression, other._schema.CommandRouteExpression, StringComparison.OrdinalIgnoreCase);
    }


    public override string ToString() => _schema.CommandRouteExpression;

    public string GetHelpText() => _schema.GetHelpText();

    public ICliActionSchema GetSchema() => _schema;
}