﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class JazzArgumentInfo : ICliRouteSegment
{
    private readonly IReadOnlyList<ICliRouteSegment> _routeSegments;

    public JazzArgumentInfo(
        CliRouteArgumentAttribute argument, 
        ParameterInfo parameter,
        IReadOnlyList<ICliRouteSegment> routeSegments)
    {
        _routeSegments = routeSegments;
    }

    public string RegexMatchGroupName { get; }


    public object? Deserialize(Match commandlineMatch, CliTokenDecoder decoder)
    {
        throw new System.NotImplementedException();
    }

    public string BuildRegularExpression()
    {
        var index = _routeSegments
            .Select((seg, i) => ReferenceEquals(this, seg) ? i : -1)
            .Where(i => i >= 0)
            .FirstOrDefault(-1);
        ThrowIf.False(index >= 0, "Oops...");

        var subCommandExpression = _routeSegments
            .OfType<CliSubCommandInfo>()
            .Select(sc => sc.BuildRegularExpression())
            .Select(exp => $"(?:{exp})")
            .Join("|")
            .Convert(exp => $"(?:{exp})");


        return _routeSegments
            .Take(index)
            .Select(cs =>
            {
                if (cs is CliSubCommandInfo subCommand)
                {
                    return subCommand.BuildRegularExpression();
                }
                if (cs is JazzArgumentInfo argument)
                {
                    return @$"(?!{subCommandExpression})(?!-)\S+";
                }

                throw new InvalidOperationException();
            })
            .Select(p => $"(?:{p})")
            .Join("\\s+")
            .Convert(p => p.IsPrintable() ? @$"(?<={p}\s+)" : string.Empty)
            .Convert(lookBehindExpression
                =>
            {
                var lookAheadExpression = $"(?!{subCommandExpression})(?!-)";
                return @$"{lookBehindExpression}{lookAheadExpression}(?<{RegexMatchGroupName}>\S+)";
            });

    }
}