using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliMethodInfo : MethodInfoDecorator
{
    private CliMethodInfo(MethodInfo method) : base(method)
    {
        Debug.Assert(IsCliMethod(method));
        var parameters = GetParameters();
        var arguments = GetCustomAttributes<CliArgumentAttribute>(true).ToList();

        var argumentParameterPairs = arguments
            .Select(arg => KeyValuePair
                .Create(
                    parameters.FirstOrDefault(arg.References),
                    arg))
            .ToList();

        var argumentByParameter = argumentParameterPairs
            .Where(p => p.Key is not null)
            .ToDictionary(p => p.Key!, p => p.Value);

        var cliParameters = new List<ParameterInfoDecorator>(parameters.Length);
        foreach (var parameter in parameters)
        {
            if (argumentByParameter.TryGetValue(parameter, out var arg))
            {
                cliParameters.Add(new CliArgumentParameterInfo(parameter, arg));
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

    public ImmutableArray<ParameterInfoDecorator> CliParameters { get; }

    private static bool IsCliMethod(MethodInfo methodInfo)
    {
        throw new NotImplementedException();
    }
}