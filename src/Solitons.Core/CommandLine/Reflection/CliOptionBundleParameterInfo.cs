using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Solitons.CommandLine.Reflection;

internal class CliOptionBundleParameterInfo : CliParameterInfo
{
    private readonly ImmutableArray<CliOptionBundlePropertyInfo> _properties;
    public CliOptionBundleParameterInfo(ParameterInfo parameter) : base(parameter)
    {
        ThrowIf.False(CliOptionBundle.IsAssignableFrom(parameter.ParameterType));
        CliOptionBundleType = parameter.ParameterType;
        var properties = CliOptionBundleType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(CliOptionBundlePropertyInfo.IsBundleProperty)
            .Select(p => new CliOptionBundlePropertyInfo(p))
            .ToList();
        _properties = [.. properties];
    }

    public Type CliOptionBundleType { get; }

    public override object Parse(string arg)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<CliOptionBundlePropertyInfo> GetOptions() => _properties;

    public object? GetValue(CliCommandLine commandLine)
    {
        var bundle = Activator.CreateInstance(CliOptionBundleType);
        foreach (var property in _properties)
        {
            var value = property.Materialize(commandLine);
            property.SetValue(bundle, value);

        }
        return bundle;
    }
}