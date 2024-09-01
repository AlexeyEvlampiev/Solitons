using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    private readonly object? _instance;
    private readonly MethodInfo _method;
    private readonly CliMasterOptionBundle[] _masterOptions;

    private readonly CliActionSchema _schema;
    private readonly CliCommandOperandCollection _operands;

    internal CliAction(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptions)
    {
        _instance = instance;
        _method = ThrowIf.ArgumentNull(method);

        _operands = new CliCommandOperandCollection(method);
        
        _masterOptions = ThrowIf.ArgumentNull(masterOptions);
        var attributes = method.GetCustomAttributes().ToArray();
        Examples = [..attributes.OfType<CliCommandExampleAttribute>()];

        Description = attributes
            .OfType<DescriptionAttribute>()
            .Select(d => d.Description)
            .FirstOrDefault(string.Empty);

        _schema = new CliActionSchema(builder =>
        {
            // Sequence is very important.
            // First: commands and arguments in the order of their declaration.
            // Second: command options
            foreach (var att in attributes)
            {
                if (att is CliCommandAttribute cmd)
                {
                    cmd.ForEach(sc => builder
                        .AddSubCommand(sc.Aliases));
                }

                if (att is CliArgumentAttribute arg)
                {
                    builder.AddArgument(arg.ParameterName);
                }
            }

            foreach (var option in _operands.GetAllCommandOptions())
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

    public ImmutableArray<CliCommandExampleAttribute> Examples { get; }


    public string Description { get; }

    public string CommandFullPath => _schema.CommandFullPath;


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

        var args = _operands.BuildMethodArguments(match, preProcessor);

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
        var args = Environment.GetCommandLineArgs();
        var tool = args[0];
        if (File.Exists(tool))
        {
            tool = Path.GetFileName(tool);
        }

        var help = CliActionHelpRtt.Build(tool, this);
        Console.WriteLine(help);
    }


    public int CompareTo(CliAction? other)
    {
        ThrowIf.ArgumentNull(other, "Cannot compare to a null object.");
        return String.Compare(CommandFullPath, other?.CommandFullPath, StringComparison.OrdinalIgnoreCase);
    }


    public override string ToString()
    {
        return Examples
            .Select(e => e.Example)
            .FirstOrDefault()
            .DefaultIfNullOrWhiteSpace(_method.Name);
    }
}