﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Collections;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliMethodInfo : MethodInfoDecorator
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


    public int Invoke(object?  instance, CliCommandLine? commandLine)
    {
        throw new NotImplementedException();
    }

    public new ImmutableArray<ParameterInfoDecorator> GetParameters() => _parameters;

    public ImmutableArray<CliCommandExampleAttribute> Examples { get; }

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
                        .Split(route.RouteDeclaration, @"\s+")
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
        double result = 0;


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
}