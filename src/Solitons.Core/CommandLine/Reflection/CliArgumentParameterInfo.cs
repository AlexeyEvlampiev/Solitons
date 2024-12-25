using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliArgumentParameterInfo : CliParameterInfo
{
    private readonly CliArgumentAttribute _argument;

    public CliArgumentParameterInfo(
        ParameterInfo parameter, 
        CliArgumentAttribute argument,
        int cliRoutePosition) : base(parameter)
    {
        _argument = argument;
        if (false == argument.CanAccept(parameter.ParameterType, out var typeConverter))
        {
            throw new InvalidOperationException("Oops...");
        }

        if (cliRoutePosition < 0)
        {
            throw new InvalidOperationException("Oops...");
        }

        var attributes = GetCustomAttributes(true).ToArray();

        TypeConverter = typeConverter;
        Description = attributes
            .OfType<DescriptionAttribute>()
            .Select(a => a.Description)
            .FirstOrDefault(argument.Description)
            .DefaultIfNullOrWhiteSpace(argument.Name);
        CliRoutePosition = cliRoutePosition;

    }

    public string Description { get; }

    public TypeConverter TypeConverter { get; }

    public int CliRoutePosition { get; }

    public string CliArgumentName => _argument.Name;


    public override object? Materialize(CliCommandLine commandLine)
    {
        throw new NotImplementedException();
    }
}