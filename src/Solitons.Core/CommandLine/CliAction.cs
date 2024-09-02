using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    private readonly Func<object?[], Task> _asyncHandler;
    private readonly CliMasterOptionBundle[] _masterOptions;

    private readonly ICliActionSchema _schema;
    private readonly ICliCommandMethodParametersBuilder _parametersBuilder;

    [DebuggerNonUserCode]
    internal CliAction(
        Func<object?[], Task> asyncHandler,
        ICliActionSchema schema,
        ICliCommandMethodParametersBuilder parametersBuilder,
        CliMasterOptionBundle[] masterOptions)
    {
        _asyncHandler = asyncHandler;
        _schema = schema;
        _parametersBuilder = parametersBuilder;
        _masterOptions = masterOptions;
    }


    internal static CliAction Create(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptions)
    {
        [DebuggerStepThrough]
        Task Invoke(object?[] args) => method.InvokeAsync(instance, args);

        var parametersBuilder = new CliCommandMethodParametersBuilder(method);

        masterOptions = ThrowIf.ArgumentNull(masterOptions);
        var attributes = method.GetCustomAttributes().ToArray();


        var schema = new CliActionSchema(builder =>
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

            foreach (var option in parametersBuilder.GetAllCommandOptions())
            {
                builder.AddOption(option.OptionLongName, option.OperandArity, option.OptionAliases);
            }

            foreach (var masterOption in masterOptions.SelectMany(mo => mo.GetAllCommandOptions()))
            {
                builder.AddOption(masterOption.OptionLongName, masterOption.OperandArity, masterOption.OptionAliases);
            }

            foreach (var example in attributes.OfType<CliCommandExampleAttribute>())
            {
                builder.AddExample(example.Example, example.Description);
            }
        });

        return new CliAction(Invoke, schema, parametersBuilder, masterOptions);
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

        var args = _parametersBuilder.BuildMethodArguments(match, preProcessor);

        foreach (var bundle in _masterOptions)
        {
            bundle.PopulateFrom(match, preProcessor);
        }

        _masterOptions.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var task = _asyncHandler.Invoke(args);
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