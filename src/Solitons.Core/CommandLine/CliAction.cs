﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Solitons.CommandLine;

internal sealed class CliAction : IComparable<CliAction>
{
    internal delegate Task<int> ActionHandler(object?[] args);

    delegate Match MatchHandler(
        string commandLine, 
        CliTokenDecoder decoder, 
        Action<IEnumerable<string>> handleUnmatchedTokens);

    delegate int CalcRankHandler(string commandLine);
    delegate bool IsMatchHandler(string commandLine);
    delegate string GetHelp(string executableName);

    private readonly CliDeserializer[] _parameterDeserializers;
    private readonly CliMasterOptionBundle[] _masterOptionBundles;
    private readonly ActionHandler _handler;
    private readonly MatchHandler _match;
    private readonly IsMatchHandler _isMatch;
    private readonly CalcRankHandler _calcRank;
    private readonly GetHelp _help;




    internal CliAction(
        ActionHandler handler,
        CliDeserializer[] parameterDeserializers,
        CliMasterOptionBundle[] masterOptionBundles,
        IReadOnlyList<ICliRouteSegment> route,
        IReadOnlyList<JazzyOptionInfo> options,
        IReadOnlyList<IJazzExampleMetadata> examples)
    {
        _parameterDeserializers = parameterDeserializers;
        _masterOptionBundles = masterOptionBundles;
        _handler = ThrowIf.ArgumentNull(handler);
        RegularExpression = CliActionRegularExpressionRtt.ToString(route, options);
        RankerRegularExpression = CliActionRegexMatchRankerRtt.ToString(route, options);

        var regex = new Regex(
            RegularExpression,
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Singleline |
            RegexOptions.IgnoreCase);
        
        var rankerRegex = new Regex(
            RankerRegularExpression,
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Singleline |
            RegexOptions.IgnoreCase);

        [DebuggerStepThrough]
        Match MatchCommandLine(
            string commandLine, 
            CliTokenDecoder decoder, 
            Action<IEnumerable<string>> handleUnmatchedTokens)
        {
            var match = regex.Match(commandLine);
            var unmatchedTokens = match
                .Groups[CliActionRegularExpressionRtt.UnrecognizedToken]
            .Captures
                .Select(c => decoder(c.Value))
                .ToList();
            if (unmatchedTokens.Any())
            {
                handleUnmatchedTokens(unmatchedTokens);
            }
            return match;
        }

        int CalcRank(string commandLine)
        {
            var match = rankerRegex.Match(commandLine);
            var groups = match.Groups
                .OfType<Group>()
                .Where(g => g.Success)
                .Skip(1) // Exclude group 0 from count
                .ToList();
            int rank = groups.Count;
            return rank;
        }

        string GetHelp(string executableName)
        {
            return CliActionHelpRtt
                .ToString(
                    Description.DefaultIfNullOrWhiteSpace(""),
                    executableName,
                    route, 
                    options, 
                    examples);
        }

        _match = MatchCommandLine;
        _isMatch = regex.IsMatch;
        _calcRank = CalcRank;
        _help = GetHelp;
    }

    internal static CliAction Create(
        object? instance,
        MethodInfo method,
        CliMasterOptionBundle[] masterOptionBundles,
        CliRouteAttribute[] baseRoutes)
    {
        ThrowIf.ArgumentNull(method);
        ThrowIf.ArgumentNull(masterOptionBundles);
        ThrowIf.ArgumentNull(baseRoutes);

        var parameters = method.GetParameters();
        var parameterDeserializers = new CliDeserializer[parameters.Length];

        var methodAttributes = method.GetCustomAttributes().ToList();
       

        var routeSegments = new List<ICliRouteSegment>(10);
        var arguments = new Dictionary<ParameterInfo, JazzArgumentInfo>();
        foreach (var attribute in methodAttributes)
        {
            if (attribute is CliRouteAttribute route)
            {
                routeSegments.AddRange(route);
            }
            else if(attribute is CliRouteArgumentAttribute argument)
            {
                try
                {
                    var parameter = parameters.Single(argument.References);
                    var type = parameter.ParameterType;
                    if (CliOptionBundle.IsAssignableFrom(type))
                    {
                        throw new CliConfigurationException(
                            $"The parameter '{argument.ParameterName}' in the '{method.Name}' method is an option bundle of type '{type}'. " +
                            $"Option bundles cannot be marked as command arguments. Review the method signature and ensure that bundles are handled correctly.");
                    }
                    var isOption = parameter
                        .GetCustomAttributes()
                        .OfType<CliOptionAttribute>()
                        .Any();
                    if (isOption)
                    {
                        throw new CliConfigurationException(
                            $"The parameter '{parameter.Name}' in the '{method.Name}' method is marked as both a command-line option and " +
                            $"a command-line argument. A parameter cannot be marked as both. Please review the attributes applied to this parameter."
                        );
                    }

                    var argumentInfo = new JazzArgumentInfo(argument, parameter, routeSegments);
                    routeSegments.Add(argumentInfo);
                    arguments.Add(parameter, argumentInfo);

                    var parameterIndex = Array.IndexOf(parameters, parameter);
                    ThrowIf.False(parameterIndex >= 0);
                    parameterDeserializers[parameterIndex] = argumentInfo.Deserialize;
                }
                catch (InvalidOperationException e)
                {
                    var refCount = parameters.Where(argument.References).Count();
                    if (refCount == 0)
                    {
                        throw new CliConfigurationException(
                            $"The parameter '{argument.ParameterName}', referenced by the '{argument.GetType().Name}' attribute in the '{method.Name}' method, " +
                            $"could not be found in the method signature. " +
                            $"Verify that the parameter name is correct and matches the method's defined parameters.");
                    }

                    if (refCount > 1)
                    {
                        throw new CliConfigurationException(
                            $"The parameter '{argument.ParameterName}' in the '{method.Name}' method is referenced by more than one '{argument.GetType().Name}' attribute. " +
                            $"Each parameter can only be referenced by one attribute. Ensure that only a single attribute is applied to each method parameter.");
                    }

                    throw;
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }



        var options = new List<JazzyOptionInfo>();
        for (int i = 0; i < parameters.Length; ++i)
        {
            var parameter = parameters[i];
            var parameterAttributes = parameter.GetCustomAttributes().ToList();
            if (arguments.ContainsKey(parameter))
            {
                Debug.Assert(false == parameterAttributes.OfType<CliOptionAttribute>().Any());
                Debug.Assert(false == CliOptionBundle.IsAssignableFrom(parameter.ParameterType));
                continue;
            }

            
            var optionAttribute = parameterAttributes.OfType<CliOptionAttribute>().SingleOrDefault();
            var description = parameterAttributes
                .OfType<DescriptionAttribute>()
                .Select(attribute => attribute.Description)
                .Union(parameterAttributes.OfType<CliOptionAttribute>().Select(attribute => attribute.Description))
                .Union([$"'{method.Name}' method parameter."])
                .First();

            bool isBundle = CliOptionBundle.IsAssignableFrom(parameter.ParameterType);

            if (isBundle)
            {
                if (optionAttribute is not null)
                {
                    throw new CliConfigurationException(
                        $"The parameter '{parameter.Name}' in the '{method.Name}' method is an option bundle of type '{parameter.ParameterType}'. " +
                        $"Option bundles cannot be marked as individual options. Review the method signature and ensure that bundles are handled correctly.");
                }
                parameterDeserializers[i] = CliOptionBundle.CreateDeserializerFor(parameter.ParameterType);
                options.AddRange(CliOptionBundle.GetOptions(parameter.ParameterType));
            }
            else
            {
                optionAttribute ??= new CliOptionAttribute(
                    ThrowIf.NullOrWhiteSpace(parameter.Name),
                    description);

                var option = new JazzyOptionInfo(
                    ThrowIf.NullReference(optionAttribute),
                    parameter.DefaultValue,
                    description,
                    parameter.ParameterType)
                {
                    IsRequired = (parameter.HasDefaultValue == false) ||
                                 parameterAttributes.OfType<RequiredAttribute>().Any()
                };
                options.Add(option);
                parameterDeserializers[i] = option.Deserialize;
            }

        }

        foreach (var bundle in masterOptionBundles)
        {
            options.AddRange(bundle.GetOptions());
        }

        var examples = methodAttributes.OfType<IJazzExampleMetadata>().ToList();
        return new CliAction(
            InvokeAsync, 
            parameterDeserializers.ToArray(), 
            masterOptionBundles,
            routeSegments,
            options,
            examples)
        {
            Description = ""
        };

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

    public string RegularExpression { get; }

    public string RankerRegularExpression { get; }

    public required string Description { get; init; }

    public int Execute(string commandLine, CliTokenDecoder decoder)
    {
        commandLine = ThrowIf.ArgumentNullOrWhiteSpace(commandLine);
        var match = _match(
            commandLine,
            decoder,
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

        var args = new object?[_parameterDeserializers.Length];
        for (int i = 0; i < args.Length; ++i)
        {
            var deserializer = _parameterDeserializers[i];
            args[i] = deserializer.Invoke(match, decoder);
        }

        var masterBundles = _masterOptionBundles.Select(bundle => bundle.Clone()).ToList();
        foreach (var bundle in masterBundles)
        {
            bundle.PopulateOptions(match, decoder);
        }

        masterBundles.ForEach(bundle => bundle.OnExecutingAction(commandLine));
        try
        {
            var task = _handler.Invoke(args);
            task.GetAwaiter().GetResult();
            var resultProperty = task.GetType().GetProperty("Result");
            object result = 0;
            if (resultProperty != null)
            {
                result = resultProperty.GetValue(task) ?? 0;
            }

            masterBundles.ForEach(bundle => bundle.OnActionExecuted(commandLine));
            
            return result is int exitCode ? exitCode : 0;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.GetType().Name);
            masterBundles.ForEach(bundle => bundle.OnError(commandLine, e));
            throw;
        }


    }

    [DebuggerStepThrough]
    public int Rank(string commandLine) => _calcRank(commandLine);


    [DebuggerStepThrough]
    public bool IsMatch(string commandLine) => _isMatch(commandLine);

    public void ShowHelp()
    {
        Console.WriteLine(GetHelpText());
    }


    public int CompareTo(CliAction? other)
    {
        other = ThrowIf.ArgumentNull(other, "Cannot compare to a null object.");
        return String.Compare(RegularExpression, other.RegularExpression, StringComparison.OrdinalIgnoreCase);
    }


    public override string ToString() => _help("");

    public string ToString(string executableName) => _help(executableName);

    public string GetHelpText(string executableName) => _help(executableName);

}