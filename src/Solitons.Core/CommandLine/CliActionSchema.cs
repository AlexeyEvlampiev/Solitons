using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

/// <summary>
/// Represents a schema for defining and parsing command-line actions.
/// </summary>
internal sealed class CliActionSchema : ICliActionSchema
{
    private readonly List<object> _fields = new();
    private readonly Regex _regex;
    private readonly Regex _rankRegex;

    [DebuggerStepThrough]
    internal CliActionSchema(MethodInfo method) : this(method, [], []) { }

    internal CliActionSchema(
        MethodInfo method, 
        IReadOnlyList<CliMasterOptionBundle> masterOptionBundles,
        IEnumerable<CliRouteAttribute> baseRoutes)
    {
        ThrowIf.ArgumentNull(method);
        var methodAttributes = method.GetCustomAttributes().ToList();
        var parameters = method.GetParameters();

        _fields.AddRange(baseRoutes
            .SelectMany(route => route)
            .Distinct()
            .Select(subCommand => new RouteSubCommand(subCommand.Aliases)));

        CommandDescription = methodAttributes
            .OfType<DescriptionAttribute>()
            .Select(a => a.Description)
            .SingleOrDefault($"Invokes {method.Name} method.");

        Examples = methodAttributes
            .OfType<CliCommandExampleAttribute>()
            .Select(a => new Example(a.Example, a.Description))
            .ToArray();

        var routeArgumentAttributes = methodAttributes
            .OfType<CliRouteArgumentAttribute>()
            .ToList();

        foreach (var attribute in methodAttributes)
        {
            if (attribute is CliRouteAttribute route)
            {
                route.ForEach(subCommand => _fields.Add(new RouteSubCommand(subCommand.Aliases)));
            }

            if (attribute is CliRouteArgumentAttribute argument)
            {
                _fields.Add(new RouteArgument(argument.ParameterName, argument.ArgumentRole, _fields.OfType<RouteSubCommand>() ));
                if (false == parameters.Any(argument.References))
                {
                    throw new InvalidOperationException($"Too bad");
                }
                if(parameters.Where(argument.References).Count() > 1)
                {
                    throw new InvalidOperationException($"Oh my...");
                }
            }
        }

        routeArgumentAttributes
            .Where(argument => false == parameters.Any(argument.References))
            .Select(argument => argument.ParameterName)
            .Join(",")
            .Convert(csv =>
            {
                if (csv.IsPrintable())
                {
                    throw new InvalidOperationException($"Oh no...");
                }
            });


        
        foreach (var parameter in parameters)
        {
            if (routeArgumentAttributes.Any(a => a.References(parameter)))
            {
                continue;
            }
            if (CliOptionBundle.IsAssignableFrom(parameter.ParameterType))
            {
                RegisterOptionsBundle(parameter.ParameterType);
            }
            else
            {
                var parameterName = ThrowIf.NullOrWhiteSpace(parameter.Name);
                var parameterAttributes = parameter
                    .GetCustomAttributes()
                    .ToList();
                var description = parameterAttributes
                    .OfType<DescriptionAttribute>()
                    .Select(a => a.Description)
                    .FirstOrDefault($"{method.Name} method parameter.");
                var option = parameterAttributes
                    .OfType<CliOptionAttribute>()
                    .SingleOrDefault() ?? new CliOptionAttribute($"--{parameterName.ToLowerInvariant()}", description);

                var optionArity = CliUtils.GetOptionArity(parameter.ParameterType);
                var underlyingType = CliUtils.GetUnderlyingType(parameter.ParameterType);

                var typeConverter = parameterAttributes
                    .OfType<TypeConverterAttribute>()
                    .Select(a => Type.GetType(a.ConverterTypeName) ?? throw new InvalidOperationException("Oops"))
                    .Select(Activator.CreateInstance)
                    .OfType<TypeConverter>()
                    .Concat([option.GetCustomTypeConverter() ?? TypeDescriptor.GetConverter(underlyingType)])
                    .First();

                if (typeConverter.CanConvertTo(underlyingType))
                {
                    _fields.Add(new Option(
                        $"{parameter.Name}_{OptionsCount:000}",
                        option.Aliases,
                        optionArity,
                        description));
                }
                else
                {
                    throw new InvalidOperationException("No way ");
                }
            }
        }

        foreach (var bundleType in masterOptionBundles.Select(b => b.GetType()).Distinct())
        {
            RegisterOptionsBundle(bundleType);
        }


        var pattern = new CliActionRegularExpressionRtt(this)
            .ToString()
            .Convert(Beautify);

        var rankPattern = new CliActionRegexMatchRankerRtt(this)
            .ToString()
            .Convert(Beautify);

        _regex = new Regex(pattern,
            RegexOptions.Compiled |
            RegexOptions.Singleline |
            RegexOptions.IgnorePatternWhitespace);


        _rankRegex = new Regex(rankPattern,
            RegexOptions.Compiled |
            RegexOptions.Singleline |
            RegexOptions.IgnorePatternWhitespace);

        CommandRouteExpression = ThrowIf.NullOrWhiteSpace(null);
        string Beautify(string exp)
        {
#if DEBUG
            exp = Regex.Replace(exp, @"(?<=\S)[^\S\r\n]{2,}", " ");
            exp = Regex.Replace(exp, @"(?<=\n)\s*\n", "");
#endif
            return exp;
        }


        Debug.Assert(methodAttributes
            .OfType<CliRouteArgumentAttribute>()
            .All(argument => parameters
                .Count(argument.References) == 1));
    }

    public IEnumerable<ICommandSegment> CommandSegments => _fields
        .OfType<ICommandSegment>();


    public IEnumerable<IOption> Options => _fields
        .OfType<IOption>();

    public string CommandRouteExpression { get; }


    public string CommandDescription { get; }
    public IReadOnlyList<Example> Examples { get; }

    public int OptionsCount => _fields.OfType<Option>().Count();


    private void RegisterOptionsBundle(Type bundleType)
    {
        Debug.Assert(CliOptionBundle.IsAssignableFrom(bundleType));

        var properties = bundleType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            if (CliOptionBundle.IsAssignableFrom(property.PropertyType))
            {
                throw new InvalidOperationException("Nested option bundles not allowed");
            }

            var propertyAttributes = property
                .GetCustomAttributes()
                .ToList();
            var option = propertyAttributes
                .OfType<CliOptionAttribute>()
                .SingleOrDefault();
            if (option is null)
            {
                continue;
            }

            var optionArity = CliUtils.GetOptionArity(property.PropertyType);
            var underlyingType = CliUtils.GetUnderlyingType(property.PropertyType);
            var description = propertyAttributes
                .OfType<DescriptionAttribute>()
                .Select(a => a.Description)
                .FirstOrDefault($"{bundleType}.{property.Name}");
            var typeConverter = propertyAttributes
                .OfType<TypeConverterAttribute>()
                .Select(a => Type.GetType(a.ConverterTypeName) ?? throw new InvalidOperationException("Oops"))
                .Select(Activator.CreateInstance)
                .OfType<TypeConverter>()
                .Concat([
                    option.GetCustomTypeConverter() ?? TypeDescriptor.GetConverter(underlyingType)
                ])
                .First();
            if (optionArity == CliOptionArity.Flag)
            {
                typeConverter ??= new CliFlagConverter();
            }
            if (typeConverter.CanConvertTo(underlyingType))
            {
                _fields.Add(new Option(
                    $"{property.Name}_{OptionsCount:000}",
                    option.Aliases,
                    optionArity,
                    description));
            }
            else
            {
                throw new InvalidOperationException("No way ");
            }

        }
    }

    

    /// <summary>
    /// Matches the specified command line string against the schema.
    /// </summary>
    /// <param name="commandLine">The command line string to match.</param>
    /// <param name="preProcessor"></param>
    /// <param name="unrecognizedTokensHandler"></param>
    /// <returns>A <see cref="Match"/> object that contains information about the match.</returns>
    public Match Match(
        string commandLine, 
        ICliTokenSubstitutionPreprocessor preProcessor,
        Action<ISet<string>> unrecognizedTokensHandler)
    {
        var match = _regex.Match(commandLine);
        var unrecognizedParameterGroup = GetUnrecognizedTokens(match);
        if (unrecognizedParameterGroup.Success)
        {
            var unrecognizedTokens = unrecognizedParameterGroup
                .Captures
                .Select(c => c.Value.Trim())
                .Select(preProcessor.GetSubstitution)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            unrecognizedTokensHandler.Invoke(unrecognizedTokens);
        }
        return match;
    }

    [DebuggerNonUserCode]
    public bool IsMatch(string commandLine) => _regex.IsMatch(commandLine);

    /// <summary>
    /// Calculates the rank of the specified command line based on the schema.
    /// </summary>
    /// <param name="commandLine">The command line string to rank.</param>
    /// <returns>An integer representing the rank.</returns>
    public int Rank(string commandLine)
    {
        string pattern = new CliActionRegexMatchRankerRtt(this);
#if DEBUG
        pattern = Regex.Replace(pattern, @"(?<=\S)[^\S\r\n]{2,}", " ");
        pattern = Regex.Replace(pattern, @"(?<=\n)\s*\n", "");
#endif
        var regex = new Regex(pattern,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Singleline);

        var match = _rankRegex.Match(commandLine);
        var groups = match.Groups
            .OfType<Group>()
            .Where(g => g.Success)
            .Skip(1) // Exclude group 0 from count
            .ToList();
        int rank = groups.Count;
        return rank;
    }



    public interface ICommandSegment
    {
        string BuildRegularExpression();

        public sealed bool IsArgument => this is RouteArgument;
    }

    public interface IOption
    {
        string BuildRegularExpression();
    }

    public abstract class Token(IEnumerable<string> aliases)
    {
        public IReadOnlyList<string> Aliases { get; } = aliases.ToArray();

        public override string ToString() => Aliases.Join("|");
    }

    public sealed record RouteArgument(string RegexGroupName, string Role, IEnumerable<ICommandSegment> SubCommands) : ICommandSegment
    {
        public string BuildRegularExpression()
        {
            var segments = SubCommands.ToList();
            var selfIndex = segments.IndexOf(this);
            if (selfIndex == -1)
            {
                throw new InvalidOperationException();
            }

            var preCondition = segments
                .Take(selfIndex)
                .Select(cs => cs.BuildRegularExpression())
                .Select(p => $"(?:{p})")
                .Join("\\s+")
                .Convert(p => p.IsPrintable() ? @$"(?<={p}\s+)" : string.Empty)
                .Convert(lookBehind
                    =>
                {
                    var lookAhead = segments
                        .OfType<RouteSubCommand>()
                        .Select(sc => sc.BuildRegularExpression())
                        .Join("|")
                        .Convert(x => $"(?!(?:{x}))");
                    return $"{lookBehind}{lookAhead}";
                });
            

            var postCondition = segments
                .Skip(selfIndex + 1)
                .Select(cs =>
                {
                    if (cs is RouteSubCommand cmd)
                    {
                        return cmd.BuildRegularExpression();
                    }
                    return @"[^\\s-]\\S*";
                })
                .Select(p => $"(?:{p})")
                .Join("\\s+")
                .Convert(p => p.IsPrintable() ? @$"(?=\s+(?:{p}))" : string.Empty);

            var pattern = $@"{preCondition}(?<{RegexGroupName}>[^\s-]\S*){postCondition}";
            return pattern;
        }

        public override string ToString() => $"<{Role.ToUpperInvariant()}>";
    }



    public sealed class RouteSubCommand(IEnumerable<string> aliases) : Token(aliases), ICommandSegment
    {
        public string BuildRegularExpression()
        {
            var valueExp = aliases
                .Select(a => a.Trim().ToLower())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(a => a.Length)
                .Join("|")
                .DefaultIfNullOrWhiteSpace("$?");
            return valueExp;
        }
    }


    public sealed class Option(
        string regexGroupName, 
        IEnumerable<string> aliases, 
        CliOptionArity arity,
        string description) : Token(aliases), IOption
    {
        public string RegexGroupName { get; } = regexGroupName;
        public CliOptionArity Arity { get; } = arity;
        public string Description { get; } = description;

        public string BuildRegularExpression()
        {
            var token = aliases
                .Select(a => a.Trim().ToLower())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(a => a.Length)
                .Join("|");
            ThrowIf.NullOrWhiteSpace(token);
            switch (Arity)
            {
                case (CliOptionArity.Flag):
                    return $@"(?<{RegexGroupName}>{token})";
                case (CliOptionArity.Scalar):
                    return $@"(?:{token})\s*(?<{RegexGroupName}>(?:[^\s-]\S*)?)";
                case (CliOptionArity.Map):
                {
                    var pattern = $@"(?:{token})(?:$dot-notation|$accessor-notation)"
                        .Replace(@"$dot-notation", @$"\.(?<{RegexGroupName}>(?:\S+\s+[^\s-]\S+)?)")
                        .Replace(@"$accessor-notation", @$"(?<{RegexGroupName}>(?:\[\S+\]\s+[^\s-]\S+)?)");
                    return pattern;
                }
                default:
                    throw new NotSupportedException() ;
            }
        }
    }

    private Group GetUnrecognizedTokens(Match match) => match.Groups[CliActionRegularExpressionRtt.UnrecognizedToken];

    public string GetHelpText()
    {
        throw new NotImplementedException();
    }


    public sealed record Example(string Command, string Description);
}