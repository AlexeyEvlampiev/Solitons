using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Solitons.Collections;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliMethodInfo : MethodInfoDecorator, IFormattable
{
    private readonly ImmutableArray<ParameterInfoDecorator> _parameters;
    private readonly ImmutableArray<object> _routeSegments;


    private CliMethodInfo(
        CliRouteAttribute[] baseRoutes,
        MethodInfo method) : base(method)
    {
        Debug.Assert(IsCliMethod(method));
        var parameters = base.GetParameters();
        var attributes = GetCustomAttributes(true).OfType<Attribute>().ToList();
        _routeSegments = [..ExtractRouteParts(attributes)];
        



        var arguments = attributes
            .OfType<CliArgumentAttribute>()
            .Select(attribute => new
            {
                Parameter = parameters.FirstOrDefault(attribute.References),
                Attribute = attribute,
                CliRoutePosition = _routeSegments.IndexOf(attribute)
            })
            .ToList();

        var argumentByParameter = arguments
            .Where(p => p.Parameter is not null)
            .ToDictionary(p => p.Parameter!);

        var cliParameters = new List<ParameterInfoDecorator>(parameters.Length);
        foreach (var parameter in parameters)
        {
            if (argumentByParameter.TryGetValue(parameter, out var arg))
            {
                cliParameters.Add(new CliArgumentParameterInfo(parameter, arg.Attribute, arg.CliRoutePosition));
            }
            else if (CliOptionBundle.IsAssignableFrom(parameter.ParameterType))
            {
                cliParameters.Add(new CliOptionBundleParameterInfo(parameter));
            }
            else
            {
                cliParameters.Add(new CliOptionParameterInfo(parameter));
            }

        }

        _parameters = [.. cliParameters];
        Examples = [.. attributes.OfType<CliCommandExampleAttribute>()];
        Description = attributes.OfType<DescriptionAttribute>().Select(a => a.Description).FirstOrDefault(Name);
    }

    [DebuggerStepThrough]
    public static CliMethodInfo[] Get(Type type) => Get([], type);

    public static CliMethodInfo[] Get(CliRouteAttribute[] baseRoutes, Type type)
    {
        var methods = FluentList
            .Create(type)
            .AddRange(type.GetInterfaces())
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Distinct()
            .ToList();

        var list = new List<CliMethodInfo>();

        foreach (var method in methods)
        {
            if (false == IsCliMethod(method))
            {
                continue;
            }

            list.Add(new CliMethodInfo(baseRoutes, method));
        }

        return list.ToArray();
    }

    public string Description { get; }

    public int Invoke(object? instance, CliCommandLine commandLine)
    {
        try
        {
            var args = ToMethodArguments(commandLine);
            object result = Invoke(instance, args);
            if (result is Task task)
            {
                Debug.WriteLine($"Awaiting '{Name}' returned task");
                task.GetAwaiter().GetResult();
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task) ?? 0;
                    Debug.WriteLine($"'{Name}' returned task result is '{result}'");
                }
            }

            if (result is int exitCode)
            {
                return exitCode;
            }
        }
        catch (CliOptionMaterializationException e)
        {
            throw new CliExitException(e.Message);
        }
        catch (TargetInvocationException e)
        {
            throw e.InnerException ?? new CliExitException("Internal error");
        }


        return 0;
    }

    private object?[] ToMethodArguments(CliCommandLine commandLine)
    {
        var optionInfos = GetOptions().ToList();

        var unrecognizedOptionsCsv = commandLine.Options
            .Where(o => false == optionInfos.Any(info => info.IsMatch(o.Name)))
            .Select(o => o.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Join(",");

        if (unrecognizedOptionsCsv.IsPrintable())
        {
            throw new CliExitException($"Unrecognized option(s): {unrecognizedOptionsCsv}");
        }

        object?[] args = new object?[_parameters.Length];
        for (int i = 0; i < args.Length; ++i)
        {
            var parameter = _parameters[i];
            if (parameter is CliArgumentParameterInfo argument)
            {
                var segment = commandLine.Segments[argument.CliRoutePosition];
                args[i] = segment;
            }
            else if(parameter is CliOptionParameterInfo optionInfo)
            {
                args[i] = optionInfo.Materialize(commandLine);
            }
            else if(parameter is CliOptionBundleParameterInfo optionBundleInfo)
            {
                args[i] = optionBundleInfo.Materialize(commandLine);
            }
        }

        return args;
    }

    public new ImmutableArray<ParameterInfoDecorator> GetParameters() => _parameters;

    public ImmutableArray<CliCommandExampleAttribute> Examples { get; }

    public IEnumerable<ICliOptionMemberInfo> GetOptions()
    {
        foreach (var parameter in _parameters)
        {
            if (parameter is CliOptionParameterInfo optionParameter)
            {
                yield return optionParameter;
            }
            else if(parameter is CliOptionBundleParameterInfo optionBundleParameter)
            {
                foreach (var o in optionBundleParameter.GetOptions())
                {
                    yield return o;
                }
            }
        }
    }

    private static bool IsCliMethod(MethodInfo methodInfo)
    {
        return methodInfo.GetCustomAttributes<CliRouteAttribute>().Any();
    }

    private static IReadOnlyList<object> ExtractRouteParts(IReadOnlyList<Attribute> attributes)
    {
        return attributes
            .SelectMany(attribute =>
            {
                if (attribute is CliRouteAttribute route)
                {
                    return Regex
                        .Split(route.RouteSignature, @"\s+")
                        .Where(r => r.IsPrintable())
                        .Select(r => r.Trim())
                        .Select(r => (object)new Regex(r, RegexOptions.IgnoreCase));
                }

                if (attribute is CliArgumentAttribute argument)
                {
                    return [argument];
                }

                return [];
            })
            .ToList();
    }

    public bool IsMatch(CliCommandLine commandLine)
    {
        if (_routeSegments.Length != commandLine.Segments.Length)
        {
            return false;
        }

        for (int i = 0; i < _routeSegments.Length; ++i)
        {
            var route = _routeSegments[i];
            var segment = commandLine.Segments[i];
            if (route is Regex rgx && 
                false == rgx.IsMatch(segment))
            {
                return false;
            }
        }

        return true;
    }

    public double Rank(CliCommandLine commandLine)
    {
        return RankSequence(commandLine).Sum();
    }

    private IEnumerable<double> RankSequence(CliCommandLine commandLine)
    {
        var length = Math.Max(commandLine.Segments.Length, _routeSegments.Length);
        for (int i = 0; i < length; ++i)
        {
            if (i >= commandLine.Segments.Length ||
                i >= _routeSegments.Length)
            {
                yield return -1;
                continue;
            }

            var segment = commandLine.Segments[i];
            var route = _routeSegments[i];
            if (route is Regex rgx)
            {
                yield return rgx.IsMatch(segment) ? (+1) : (-1);
                continue;
            }

            if (route is CliArgumentAttribute arg)
            {
                var matchesAnyRoute = _routeSegments.OfType<Regex>().Any(r => r.IsMatch(segment));
                yield return matchesAnyRoute ? -1 : +1;
            }
        }

        var optionInfos = GetOptions().ToList();

        var matchedOptions = optionInfos
                .Count(optionInfo => optionInfo.IsIn(commandLine));
        yield return matchedOptions;


        var missingOptions = optionInfos
            .Where(info => info.IsOptional == false)
            .Count(optionInfo => optionInfo.IsNotIn(commandLine));
        yield return missingOptions;

        var unrecognizedOptions = commandLine.Options
            .Count(optionCapture => false == optionInfos.Any(info => info.IsMatch(optionCapture.Name)));

        yield return -unrecognizedOptions;
    }

    public override string ToString()
    {
        var builder = new StringBuilder(Name);
        var parameters = 
            GetParameters()
                .Select(p =>
                {
                    if (p is CliArgumentParameterInfo)
                    {
                        return $"arg: {p.Name}";
                    }

                    if (p is CliOptionBundleParameterInfo)
                    {
                        return $"bundle: {p.Name}";
                    }

                    return p.Name;
                })!
                .Join(", ");
        builder.Append($"({parameters})");
        return builder.ToString();
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        format = format?.ToUpperInvariant();
        switch (format)
        {
            case ("GH"): return ToGeneralHelpString();
            default: return base.ToString();
        }
    }

    public double RankByOptions(CliCommandLine commandLine)
    {
        var optionInfos = GetOptions().ToList();

        var matchedOptions = optionInfos
            .Count(optionInfo => optionInfo.IsIn(commandLine));
        
        var missingOptions = optionInfos
            .Where(info => info.IsOptional == false)
            .Count(optionInfo => optionInfo.IsNotIn(commandLine));

        var unrecognizedOptions = commandLine.Options
            .Count(optionCapture => false == optionInfos.Any(info => info.IsMatch(optionCapture.Name)));

        return matchedOptions - missingOptions - unrecognizedOptions;
    }

    public string ToGeneralHelpString()
    {
        var rtt = new CliMethodInfoGeneralHelpRtt(this);
        return rtt.ToString();
    }

    internal IEnumerable<object> Segments => _routeSegments;
}

internal partial class CliMethodInfoGeneralHelpRtt
{
    private readonly CliMethodInfo _method;

    public CliMethodInfoGeneralHelpRtt(CliMethodInfo method)
    {
        _method = method;
        var signature = new StringBuilder();
        foreach (var attribute in _method.GetCustomAttributes(true))
        {
            if (attribute is CliRouteAttribute route)
            {
                signature.Append($" {route.RouteSignature}");
            }
            else if(attribute is CliArgumentAttribute argument) 
            {
                signature.Append($" <{argument.Name.ToUpperInvariant()}>");
            }
        }

        Signature = signature.ToString().Trim();
        Arguments = _method
            .GetParameters()
            .OfType<CliArgumentParameterInfo>()
            .Select(info => new Argument($"<{info.CliArgumentName.ToUpperInvariant()}>", info.Description))
            .ToArray();
        Options = _method
            .GetOptions()
            .Select(o => new Option(o.PipeSeparatedAliases, o.Description)).ToArray();

        //foreach (var option in _method.GetOptions())
        //{
        //    signature.Append($" {option.PipeSeparatedAliases}");
        //}
        //Signature = signature.ToString().Trim();
    }

    public string Description => _method.Description;

    public string Signature { get; }

    public IEnumerable<Argument> Arguments { get; }
    public IEnumerable<Option> Options { get; }

    public record Argument(string Name, string Description);
    public record Option(string Signature, string Description);
}