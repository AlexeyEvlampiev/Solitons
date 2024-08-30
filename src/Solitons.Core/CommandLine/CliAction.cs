using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    private readonly object? _instance;
    private readonly MethodInfo _method;
    private readonly CliMasterOptionBundle[] _masterOptions;

    private readonly object[] _commandSegments;
    private readonly List<CliOperandInfo> _operands = new();
    private readonly ParameterInfo[] _parameters;
    private readonly Dictionary<ParameterInfo, object> _parameterMetadata = new();
    private readonly CliActionSchema _schema = new();

    internal CliAction(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptions)
    {
        _instance = instance;
        _method = ThrowIf.ArgumentNull(method);
        _parameters = method.GetParameters();
        
        _masterOptions = ThrowIf.ArgumentNull(masterOptions);
        var attributes = method.GetCustomAttributes().ToArray();
        Examples = [..attributes.OfType<CliCommandExampleAttribute>()];

        _commandSegments = attributes
            .SelectMany(s =>
            {
                if (s is CliCommandAttribute cmd)
                {
                    return cmd.OfType<object>();
                }

                return [s];
            })
            .Where(a => a is CliCommandAttribute or CliArgumentAttribute)
            .ToArray();

        _commandSegments
            .ForEach(a =>
            {
                if (a is CliCommandAttribute cmd)
                {
                    cmd.ForEach(sc =>
                    {
                        _schema.AddSubCommand(sc.Aliases);
                    });
                }
                else if(a is CliArgumentAttribute arg)
                {
                    _schema.AddArgument(arg.ParameterName);

                    var targetParameter = _parameters
                        .FirstOrDefault(p => arg.References(p))
                            ?? throw new InvalidOperationException(
                                $"Target parameter '{arg.ParameterName}' not found in method '{_method.Name}'.");

                    var operand = new CliArgumentInfo(targetParameter, this, arg);
                    _operands.Add(operand);
                    _parameterMetadata.Add(targetParameter, operand);
                }
            });


        foreach (var pi in _parameters)
        {
            if (string.IsNullOrWhiteSpace(pi.Name))
            {
                throw new InvalidOperationException($"Anonymous parameter in method '{_method.Name}'.");
            }
            
            if (_parameterMetadata.TryGetValue(pi, out var argument))
            {
                Debug.Assert(argument is CliArgumentInfo);
                Debug.Assert(_operands.Contains(argument));
                continue;
            }

            if (CliOptionBundle.IsAssignableFrom(pi.ParameterType))
            {
                var bundleOptions = CliOptionBundle
                    .GetOptions(pi.ParameterType)
                    .ToArray();
                _operands.AddRange(bundleOptions);
                _parameterMetadata.Add(pi, bundleOptions);
            }
            else
            {
                var metadata = new CliOptionInfo(pi);
                _operands.Add(metadata);
                _parameterMetadata.Add(pi, metadata);
            }
        }

        foreach (var bundle in masterOptions)
        {
            _operands.AddRange(CliOptionBundle
                .GetOptions(bundle.GetType()));
        }


        foreach (var operand in _operands.Where(o => o is not CliArgumentInfo))
        {
            _schema.AddOption(operand.Name, operand.OptionArity, operand.Aliases);
        }

        Description = attributes
            .OfType<DescriptionAttribute>()
            .Select(d => d.Description)
            .FirstOrDefault(string.Empty);

        this.FullPath = _commandSegments
            .Select(s =>
            {
                if (s is CliSubCommand cmd)
                {
                    if (cmd.PrimaryName.IsNullOrWhiteSpace())
                    {
                        return string.Empty;
                    }
                    return cmd.SubCommandPattern;
                }

                if (s is CliArgumentAttribute a)
                {
                    return $"<{a.ArgumentRole.ToUpper()}>";
                }

                throw new InvalidOperationException("Unsupported command segment encountered.");
            })
            .Where(s => s.IsPrintable())
            .Join(" ");
    }

    public ImmutableArray<CliCommandExampleAttribute> Examples { get; }


    internal IEnumerable<object> CommandSegments => _commandSegments;

    internal IEnumerable<CliOperandInfo> Operands => _operands;
    public string Description { get; }

    public string FullPath { get; }


    public int Execute(string commandLine, CliTokenSubstitutionPreprocessor preProcessor)
    {
        commandLine = ThrowIf.ArgumentNullOrWhiteSpace(commandLine);
        var match = _schema.Match(commandLine);
        if (match.Success == false)
        {
            throw new InvalidOperationException($"The command line did not match any known patterns.");
        }

        var unrecognizedParameterGroup = _schema.GetUnrecognizedTokens(match);
        if (unrecognizedParameterGroup.Success)
        {
            var csv = unrecognizedParameterGroup
                .Captures
                .Select(c => c.Value.Trim())
                .Select(preProcessor.GetSubstitution)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Join(", ");
            throw new CliExitException(
                $"The following options are not recognized as valid for the command: {csv}. " +
                $"Please check the command syntax.");
        }

        var args = new object?[_parameters.Length];
        for (int i = 0; i < args.Length; ++i)
        {
            var parameter = _parameters[i];
            if (CliOptionBundle.IsAssignableFrom(parameter.ParameterType))
            {
                var bundle = Activator
                    .CreateInstance(parameter.ParameterType)
                    .Convert(o => ThrowIf.NullReference(o))
                    .Convert(o => (CliOptionBundle)o);
                args[i] = bundle;
                bundle.Populate(match, preProcessor);
            }
            else if (_parameterMetadata.TryGetValue(parameter, out var metadata))
            {
                if (metadata is CliParameterInfo option)
                {
                    Debug.WriteLine(option.Name);
                    args[i] = option.GetValue(match, preProcessor);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unexpected metadata type for parameter '{parameter.Name}' in method '{_method.Name}'.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"No metadata found for parameter '{parameter.Name}' in method '{_method.Name}'.");
            }
        }

        foreach (var bundle in _masterOptions)
        {
            bundle.Populate(match, preProcessor);
        }

        _masterOptions.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var result = _method.Invoke(_instance, args);
            if (result is Task task)
            {
                task.GetAwaiter().GetResult();
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task);
                }
            }

            _masterOptions.ForEach(bundle => bundle.OnActionExecuted(commandLine));
            if (result is int code)
            {
                return code;
            }

            return 0;
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
    public Match Match(string commandLine) => _schema.Match(commandLine);

    [DebuggerStepThrough]
    public bool IsMatch(string commandLine) => _schema.Match(commandLine).Success;

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
        return String.Compare(FullPath, other?.FullPath, StringComparison.OrdinalIgnoreCase);
    }


    internal int IndexOfSegment(object segment) => Array.IndexOf(_commandSegments, segment);

    public override string ToString()
    {
        return Examples
            .Select(e => e.Example)
            .FirstOrDefault()
            .DefaultIfNullOrWhiteSpace(_method.Name);
    }
}