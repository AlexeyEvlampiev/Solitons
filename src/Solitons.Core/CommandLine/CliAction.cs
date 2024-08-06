using Solitons.CommandLine.ZapCli;
using System;
using System.Collections.Generic;
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

    private readonly ICliCommandSegment[] _commandSegments;
    private readonly List<CliOperandInfo> _operands = new();
    private readonly CliOptionBundle[] _bundles;
    private readonly ParameterInfo[] _parameters;
    private readonly Dictionary<ParameterInfo, object> _parameterMetadata = new();
    private readonly Regex _commandExactRegex;
    private readonly Regex _commandFuzzyRegex;

    internal CliAction(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptions)
    {
        _instance = instance;
        _method = method;
        _parameters = method.GetParameters();
        
        _masterOptions = masterOptions;
        var attributes = method.GetCustomAttributes().ToArray();
        _commandSegments = attributes
            .SelectMany(attribute =>
            {
                var commandSegments = new List<ICliCommandSegment>();
                if (attribute is CliCommandAttribute command)
                {
                    commandSegments.AddRange(command.Select(subCommand => subCommand));
                }
                else if (attribute is CliArgumentAttribute argument)
                {
                    var targetParameter = _parameters
                        .FirstOrDefault(p => argument.References(p))
                        ?? throw new InvalidOperationException("Target parameter not found.");

                    var operand = new CliArgumentInfo(targetParameter, this, argument);
                    commandSegments.Add(operand);
                    _operands.Add(operand);
                    _parameterMetadata.Add(targetParameter, operand);
                }
                return commandSegments;
            })
            .ToArray();



        foreach (var pi in _parameters)
        {
            if (string.IsNullOrWhiteSpace(pi.Name))
            {
                throw new InvalidOperationException();
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

                if (s is CliArgumentInfo arg)
                {
                    return $"<{arg.ArgumentRole.ToUpper()}>";
                }

                throw new InvalidOperationException();
            })
            .Where(s => s.IsPrintable())
            .Join(" ");

        CommandExactMatchExpression = CliActionRegexRtt.Build(this, ZapCliActionRegexRttMode.Default);
        CommandFuzzyMatchExpression = CliActionRegexRtt.Build(this, ZapCliActionRegexRttMode.Similarity);
        _commandExactRegex = new Regex(CommandExactMatchExpression, RegexOptions.Compiled);
        _commandFuzzyRegex = new Regex(CommandFuzzyMatchExpression, RegexOptions.Compiled);
    }

    public string CommandExactMatchExpression { get; }
    public string CommandFuzzyMatchExpression { get; }

    internal IEnumerable<ICliCommandSegment> CommandSegments => _commandSegments;

    internal IEnumerable<CliOperandInfo> Operands => _operands;
    public string Description { get; }

    public string FullPath { get; }

    public bool IsMatch(string commandLine) => _commandExactRegex.IsMatch(commandLine);
    public int Execute(string commandLine, CliTokenSubstitutionPreprocessor preProcessor)
    {
        var match = _commandExactRegex.Match(commandLine);
        if (match.Success == false)
        {
            throw new InvalidOperationException();
        }

        var unrecognizedParameterGroup = CliActionRegexRtt.GetUnmatchedParameterGroup(match);
        if (unrecognizedParameterGroup.Success)
        {
            throw new NotImplementedException();
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
                    throw new InvalidOperationException();
                }
            }
            else
            {
                throw new InvalidOperationException();
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
            _masterOptions.ForEach(bundle => bundle.OnError(commandLine, e));
            throw;
        }


    }

    public CliActionSimilarity CalcSimilarity(string commandLine)
    {
        var match = _commandFuzzyRegex.Match(commandLine);
        return new CliActionSimilarity(match);
    }

    public void ShowHelp()
    {
        var args = Environment.GetCommandLineArgs();
        var tool = args[0];
        if (File.Exists(tool))
        {
            tool = System.IO.Path.GetFileName(tool);
        }

        var help = CliActionHelpRtt.Build(tool, this);
        Console.WriteLine(help);
    }

    public int Similarity(string commandLine)
    {
        throw new NotImplementedException();
    }


    public int CompareTo(CliAction? other)
    {
        ThrowIf.ArgumentNull(other);
        return String.Compare(FullPath, other?.FullPath, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAction(MethodInfo method) => method
        .GetCustomAttributes<CliCommandAttribute>()
        .Any();

    internal int IndexOf(ICliCommandSegment segment) => Array.IndexOf(_commandSegments, segment);

    public override string ToString() => $"{_method.Name}";
}