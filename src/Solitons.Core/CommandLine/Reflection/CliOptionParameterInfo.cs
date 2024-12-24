using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliOptionParameterInfo : CliParameterInfo, ICliOptionMemberInfo
{
    private readonly CliOptionAttribute _optionAttribute;
    private readonly CliOptionMaterializer _materializer;

    public CliOptionParameterInfo(ParameterInfo parameter) 
        : base(parameter)
    {
        var attributes = GetCustomAttributes(true).OfType<Attribute>().ToArray();
        _optionAttribute = attributes
            .OfType<CliOptionAttribute>()
            .Union([new CliOptionAttribute($"--{parameter.Name}")])
            .First();
        

        IsOptional = parameter.HasDefaultValue || parameter.IsNullable();
 
        _materializer = CliOptionMaterializer.CreateOrThrow(
            _optionAttribute, 
            parameter.ParameterType, 
            IsOptional, 
            parameter.DefaultValue);


        Aliases = [.. _optionAttribute.Aliases];
        Description = attributes
            .OfType<DescriptionAttribute>()
            .Select(d => d.Description)
            .Union([_optionAttribute.Description])
            .First();
    }


    public string Description { get; }

    public bool IsMatch(string optionName) => _optionAttribute.IsMatch(optionName);

    public ImmutableArray<string> Aliases { get; }

    public override bool IsOptional { get; }



    [DebuggerStepThrough]
    public override object? Materialize(CliCommandLine commandLine) => _materializer.Materialize(commandLine);

    public override string ToString() => _optionAttribute.PipeSeparatedAliases;
}