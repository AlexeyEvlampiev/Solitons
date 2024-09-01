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

    private readonly List<object> _commandSegments;
    private readonly List<CliOperandInfo> _operands = new();
    private readonly ParameterInfo[] _parametersOld;
    private readonly Dictionary<ParameterInfo, object> _parameterMetadata = new();
    private readonly CliActionSchema _schema;
    private readonly CliCommandParameterCollection _parameters;

    internal CliAction(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptions)
    {
        _instance = instance;
        _method = ThrowIf.ArgumentNull(method);

        _parameters = new CliCommandParameterCollection(method);
        _parametersOld = method.GetParameters();
        
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
            .ToList();

        _commandSegments
            .ForEach(a =>
            {
                if (a is CliCommandAttribute cmd)
                {
                    cmd.ForEach(sc =>
                    {

                    });
                }
                else if(a is CliArgumentAttribute arg)
                {


                    var targetParameter = _parametersOld
                        .FirstOrDefault(p => arg.References(p))
                            ?? throw new InvalidOperationException(
                                $"Target parameter '{arg.ParameterName}' not found in method '{_method.Name}'.");

                    var operand = new CliArgumentInfo(targetParameter, this, arg);
                    _operands.Add(operand);
                    _parameterMetadata.Add(targetParameter, operand);
                }
            });


        foreach (var pi in _parametersOld)
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

        


        Description = attributes
            .OfType<DescriptionAttribute>()
            .Select(d => d.Description)
            .FirstOrDefault(string.Empty);


        _schema = new CliActionSchema(builder =>
        {
            foreach (var operand in _operands.OfType<CliOperandInfo>())
            {
                builder.AddOption(operand.Name, operand.OperandArity, operand.Aliases);
            }
        });

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

        var args = _parameters.BuildMethodArguments(match, preProcessor);

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
        return String.Compare(FullPath, other?.FullPath, StringComparison.OrdinalIgnoreCase);
    }


    internal int IndexOfSegment(object segment) => _commandSegments.IndexOf(segment);

    public override string ToString()
    {
        return Examples
            .Select(e => e.Example)
            .FirstOrDefault()
            .DefaultIfNullOrWhiteSpace(_method.Name);
    }
}