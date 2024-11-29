using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliMethodInfo : MethodInfoDecorator
{
    private CliMethodInfo(MethodInfo method) : base(method)
    {
        Debug.Assert(IsCliMethod(method));
        var parameters = GetParameters();
        var attributes = GetCustomAttributes(true).OfType<Attribute>().ToList();
        var routeParts = ExtractRouteParts(attributes);
        



        var arguments = attributes
            .OfType<CliArgumentAttribute>()
            .Select(attribute => new
            {
                Parameter = parameters.FirstOrDefault(attribute.References),
                Attribute = attribute,
                CliRoutePosition = routeParts.IndexOf(attribute)
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


        }

        CliParameters = [.. cliParameters];
    }


    public static IEnumerable<CliMethodInfo> Get(Type type)
    {
        foreach (var method in type.GetMethods())
        {
            if (false == IsCliMethod(method))
            {
                continue;
            }

            yield return new CliMethodInfo(method);
        }
    }


    public int Invoke(CliCommandLine commandLine)
    {
        throw new NotImplementedException();
    }

    public ImmutableArray<ParameterInfoDecorator> CliParameters { get; }

    private static bool IsCliMethod(MethodInfo methodInfo)
    {
        throw new NotImplementedException();
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
                        .Select(r => (object)r.Trim());
                }

                if (attribute is CliArgumentAttribute argument)
                {
                    return [argument];
                }

                return [];
            })
            .ToList();
    }
}