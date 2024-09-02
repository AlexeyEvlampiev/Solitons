using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    private readonly object? _instance;
    private readonly MethodInfo _method;
    private readonly CliMasterOptionBundle[] _masterOptions;

    private readonly ICliActionSchema _schema;
    private readonly ICliCommandMethodParametersBuilder _methodParametersBuilders;

    internal CliAction(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptions)
    {
        _instance = instance;
        _method = ThrowIf.ArgumentNull(method);

        _methodParametersBuilders = new CliCommandMethodParametersBuilder(method);
        
        _masterOptions = ThrowIf.ArgumentNull(masterOptions);
        var attributes = method.GetCustomAttributes().ToArray();


        _schema = new CliActionSchema(builder =>
        {
            builder.Description = attributes
                .OfType<DescriptionAttribute>()
                .Select(a => a.Description)
                .FirstOrDefault(string.Empty);

            // Sequence is very important.
            // First: commands and arguments in the order of their declaration.
            // Second: command options
            foreach (var att in attributes)
            {
                if (att is CliCommandAttribute cmd)
                {
                    cmd.ForEach(subCommand => builder
                        .AddSubCommand(subCommand.Aliases));
                }

                if (att is CliArgumentAttribute arg)
                {
                    builder.AddArgument(arg.ParameterName);
                }
            }

            foreach (var option in _methodParametersBuilders.GetAllCommandOptions())
            {
                builder.AddOption(option.OptionLongName, option.OperandArity, option.OptionAliases);
            }

            foreach (var masterOption in _masterOptions.SelectMany(mo => mo.GetAllCommandOptions()))
            {
                builder.AddOption(masterOption.OptionLongName, masterOption.OperandArity, masterOption.OptionAliases);
            }

            foreach (var example in attributes.OfType<CliCommandExampleAttribute>())
            {
                builder.AddExample(example.Example, example.Description);
            }
        });

    }


    internal static CliAction Create(object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptions)
    {
        _instance = instance;
        _method = ThrowIf.ArgumentNull(method);

        _methodParametersBuilders = new CliCommandMethodParametersBuilder(method);

        _masterOptions = ThrowIf.ArgumentNull(masterOptions);
        var attributes = method.GetCustomAttributes().ToArray();


        _schema = new CliActionSchema(builder =>
        {
            builder.Description = attributes
                .OfType<DescriptionAttribute>()
                .Select(a => a.Description)
                .FirstOrDefault(string.Empty);

            // Sequence is very important.
            // First: commands and arguments in the order of their declaration.
            // Second: command options
            foreach (var att in attributes)
            {
                if (att is CliCommandAttribute cmd)
                {
                    cmd.ForEach(subCommand => builder
                        .AddSubCommand(subCommand.Aliases));
                }

                if (att is CliArgumentAttribute arg)
                {
                    builder.AddArgument(arg.ParameterName);
                }
            }

            foreach (var option in _methodParametersBuilders.GetAllCommandOptions())
            {
                builder.AddOption(option.OptionLongName, option.OperandArity, option.OptionAliases);
            }

            foreach (var masterOption in _masterOptions.SelectMany(mo => mo.GetAllCommandOptions()))
            {
                builder.AddOption(masterOption.OptionLongName, masterOption.OperandArity, masterOption.OptionAliases);
            }

            foreach (var example in attributes.OfType<CliCommandExampleAttribute>())
            {
                builder.AddExample(example.Example, example.Description);
            }
        });
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

        var args = _methodParametersBuilders.BuildMethodArguments(match, preProcessor);

        foreach (var bundle in _masterOptions)
        {
            bundle.PopulateFrom(match, preProcessor);
        }

        _masterOptions.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var task = _method.InvokeAsync(_instance, args);
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
        return String.Compare(_schema.CommandFullPath, other._schema.CommandFullPath, StringComparison.OrdinalIgnoreCase);
    }


    public override string ToString() => _schema.CommandFullPath;

    public string GetHelpText() => _schema.GetHelpText();

    public ICliActionSchema GetSchema() => _schema;
}