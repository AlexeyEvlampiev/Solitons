using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Solitons.CommandLine;

internal sealed class CliArgumentInfo : ICliRouteSegment
{
    private static long _sequenceNumber = 0;
    private readonly IReadOnlyList<ICliRouteSegment> _routeSegments;
    private readonly TypeConverter _converter;
    private readonly Type _parameterUnderlyingType;

    public CliArgumentInfo(
        CliRouteArgumentAttribute metadata, 
        ParameterInfo parameter,
        IReadOnlyList<ICliRouteSegment> routeSegments)
    {
        ThrowIf.ArgumentNull(parameter);
        _routeSegments = ThrowIf.ArgumentNull(routeSegments);
        ThrowIf.False(metadata.ParameterName.Equals(parameter.Name, StringComparison.Ordinal));

        var attributes = parameter.GetCustomAttributes().ToList();
        Metadata = metadata;

        _parameterUnderlyingType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        _converter = attributes
            .OfType<TypeConverterAttribute>()
            .Select(att => att.CreateInstance())
            .FirstOrDefault(TypeDescriptor.GetConverter(_parameterUnderlyingType));
        if (false == _converter.CanConvertFrom(typeof(string)))
        {
            throw new CliConfigurationException(
                $"The argument for parameter '{metadata.ParameterName}' of type '{_parameterUnderlyingType.FullName}' cannot be converted from a string. " +
                "Ensure that the correct type converter is provided for this CLI argument.");
        }

        Description = attributes
            .OfType<DescriptionAttribute>()
            .Select(att => att.Description)
            .Union([metadata.Description, metadata.ParameterName])
            .First(desc => desc.IsPrintable());

        RegexMatchGroupName = $"argument_{Interlocked.Increment(ref _sequenceNumber):0000}";
    }

    public string RegexMatchGroupName { get; }

    public ICliRouteArgumentMetadata Metadata { get; }

    public string Description { get; }

    public string ArgumentRole => Metadata.ArgumentRole;

    public object? Deserialize(Match commandlineMatch, CliTokenDecoder decoder)
    {
        if (false == commandlineMatch.Success)
        {
            throw new ArgumentException("The provided command line match is not valid.", nameof(commandlineMatch));
        }

        var group = commandlineMatch.Groups[RegexMatchGroupName];
        if (group.Success)
        {
            var input = decoder(group.Value);
            try
            {
                return _converter.ConvertFromInvariantString(input, _parameterUnderlyingType);
            }
            catch (InvalidOperationException)
            {
                throw new CliConfigurationException(
                    $"The conversion for parameter '{Metadata.ParameterName}' using the specified converter failed. " +
                    $"Ensure the converter and target type '{_parameterUnderlyingType.FullName}' are correct.");
            }
            catch (Exception e) when (e is FormatException or ArgumentException)
            {
                CliExit.With(
                    $"Failed to convert the input token for parameter '{Metadata.ParameterName}' " +
                    $"to the expected type '{_parameterUnderlyingType.FullName}'. Reason: {e.Message}");
                return null;
            }
        }

        CliExit.With(
            $"{Metadata.ParameterName} parameter received an invalid token which could not be converted to {_parameterUnderlyingType.FullName}."
        );
        return null;
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
                if (cs is CliArgumentInfo argument)
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

    public override string ToString() => $"<{ArgumentRole.ToUpper()}>";
}