using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Solitons.CommandLine;

internal sealed class CliArgumentInfo
{
    private readonly TypeConverter _converter;
    private readonly Type _argumentType;
    private readonly long _sequenceNumber;

    private CliArgumentInfo(
        CliRouteArgumentSegmentAttribute metadata, 
        string name, 
        string description,
        Type argumentType,
        TypeConverter converter)
    {
        SegmentMetadata = ThrowIf.ArgumentNull(metadata);
        Name = ThrowIf.ArgumentNullOrWhiteSpace(name);
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description);
        _argumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;

        _converter = converter;
        ThrowIf.False(_converter.CanConvertFrom(typeof(string)));

        RegexMatchGroupName = $"argument_{name}_{Interlocked.Increment(ref _sequenceNumber):0000}";
    }

    public static CliArgumentInfo Create(
        ICliActionSchema schema,
        int sequenceNumber,
        ParameterInfo parameter)
    {
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        var methodInfo = ThrowIf.NullReference(parameter.Member as MethodInfo);
        var name = ThrowIf.NullOrWhiteSpace(parameter.Name);

        var attributes = parameter.GetCustomAttributes().ToList();

        var description = attributes
            .OfType<DescriptionAttribute>()
            .Select(att => att.Description)
            .Union([metadata.Description, name])
            .First(desc => desc.IsPrintable());
        if (metadata.CanAccept(type, out var converter) == false || 
            converter.SupportsCliOperandConversion() == false)
        {
            throw CliConfigurationException.ArgumentTypeNotSupported(methodInfo, parameter, type, metadata);
        }

        return new CliArgumentInfo(
            metadata, 
            name, 
            description, 
            type, 
            converter);
    }

    public string Name { get; }

    public string Description { get; }

    public string RegexMatchGroupName { get; }


    public string ArgumentRole => SegmentMetadata.ArgumentRole;

    public object? Materialize(Match commandlineMatch, CliTokenDecoder decoder)
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
                return _converter.ConvertFromInvariantString(input, _argumentType);
            }
            catch (InvalidOperationException)
            {
                throw new CliConfigurationException(
                    $"The conversion for parameter '{SegmentMetadata.ParameterName}' using the specified converter failed. " +
                    $"Ensure the converter and target type '{_argumentType.FullName}' are correct.");
            }
            catch (Exception e) when (e is FormatException or ArgumentException)
            {
                throw new CliExitException(
                    $"Failed to convert the input token for parameter '{SegmentMetadata.ParameterName}' " +
                    $"to the expected type '{_argumentType.FullName}'. Reason: {e.Message}");
                return null;
            }
        }

        throw new CliExitException(
            $"{SegmentMetadata.ParameterName} parameter received an invalid token which could not be converted to {_argumentType.FullName}."
        );
    }

    public override string ToString() => $"<{ArgumentRole.ToUpper()}>";
}