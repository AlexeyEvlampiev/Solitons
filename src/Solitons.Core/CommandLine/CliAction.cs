using Solitons.CommandLine.Common;
using Solitons.CommandLine.ZapCli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

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
    private readonly Regex _regex;
    private readonly Regex _similarityRegex;

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


        DefaultPattern = CliActionRegexRtt.Build(this, ZapCliActionRegexRttMode.Default);
        SimilarityPattern = CliActionRegexRtt.Build(this, ZapCliActionRegexRttMode.Similarity);
        _regex = new Regex(DefaultPattern, RegexOptions.Compiled);
        _similarityRegex = new Regex(SimilarityPattern, RegexOptions.Compiled);
    }

    public string DefaultPattern { get; }
    public string SimilarityPattern { get; }

    internal IEnumerable<ICliCommandSegment> CommandSegments => _commandSegments;

    internal IEnumerable<CliOperandInfo> Operands => _operands;


    public bool IsMatch(string commandLine) => _regex.IsMatch(commandLine);
    public int Execute(string commandLine)
    {
        commandLine = TokenSubstitutionPreprocessor.SubstituteTokens(commandLine, out var tsp);
        var match = _regex.Match(commandLine);
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
                bundle.Populate(match);
            }
            else if (_parameterMetadata.TryGetValue(parameter, out var metadata))
            {
                if (metadata is CliParameterInfo option)
                {
                    args[i] = option.GetValue(match);
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
            bundle.Populate(match);
        }

        _masterOptions.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var result = _method.Invoke(_instance, args);
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

    public CliProcessor.Similarity CalcSimilarity(string commandLine)
    {
        var match = _similarityRegex.Match(commandLine);
        return new ZapCliActionSimilarity(match);
    }

    public void ShowHelp()
    {
        var args = Environment.GetCommandLineArgs();
        var tool = args[0];
        if (File.Exists(tool))
        {
            tool = Path.GetFileName(tool);
        }

        var help = ZapCliActionHelpRtt.Build(tool, this);
        Console.WriteLine(help);
    }

    public int Similarity(string commandLine)
    {
        throw new NotImplementedException();
    }


    public int CompareTo(CliAction? other)
    {
        throw new NotImplementedException();
    }

    public static bool IsAction(MethodInfo method) => method
        .GetCustomAttributes<CliCommandAttribute>()
        .Any();

    internal int IndexOf(ICliCommandSegment segment) => Array.IndexOf(_commandSegments, segment);
}