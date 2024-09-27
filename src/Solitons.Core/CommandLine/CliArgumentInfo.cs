using System;
using System.ComponentModel;
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
        string name, 
        string description,
        Type argumentType,
        TypeConverter converter)
    {
        Name = ThrowIf.ArgumentNullOrWhiteSpace(name);
        Description = ThrowIf.ArgumentNullOrWhiteSpace(description);
        _argumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;

        _converter = converter;
        ThrowIf.False(_converter.CanConvertFrom(typeof(string)));

        RegexMatchGroupName = $"argument_{name}_{Interlocked.Increment(ref _sequenceNumber):0000}";
    }

    public static CliArgumentInfo Create(
        CliArgumentAttribute arg,
        ParameterInfo parameter)
    {
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        var methodInfo = ThrowIf.NullReference(parameter.Member as MethodInfo);
        var name = ThrowIf.NullOrWhiteSpace(parameter.Name);


        var description = arg.Description;
        if (arg.CanAccept(type, out var converter) == false || 
            converter.SupportsCliOperandConversion() == false)
        {
            throw CliConfigurationException.ArgumentTypeNotSupported(methodInfo, parameter, type, arg);
        }

        return new CliArgumentInfo(
            arg.Name, 
            arg.Description, 
            type, 
            converter)
        {
            ParameterName = ThrowIf.NullOrWhiteSpace(parameter.Name)
        };
    }

    public string Name { get; }

    public required string ParameterName { get; init; }

    public string Description { get; }

    public string RegexMatchGroupName { get; }


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
                    $"The conversion for parameter '{ParameterName}' using the specified converter failed. " +
                    $"Ensure the converter and target type '{_argumentType.FullName}' are correct.");
            }
            catch (Exception e) when (e is FormatException or ArgumentException)
            {
                throw new CliExitException(
                    $"Failed to convert the input token for parameter '{ParameterName}' " +
                    $"to the expected type '{_argumentType.FullName}'. Reason: {e.Message}");
                return null;
            }
        }

        throw new CliExitException(
            $"{ParameterName} parameter received an invalid token which could not be converted to {_argumentType.FullName}."
        );
    }

    public override string ToString() => $"<{Name.ToUpper()}>";
}