using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive;

namespace Solitons.CommandLine.Reflection;

public interface ICliOptionMemberInfo
{
    bool IsMatch(string optionName);

    Type OptionType { get; }

    bool IsOptional { get; }

    object? DefaultValue { get; }

    TypeConverter ValueConverter { get; }

    bool IsFlag { get; }

    ImmutableArray<string> Aliases { get; }


    public sealed bool IsIn(CliCommandLine commandLine)
    {
        foreach (var option in commandLine.Options)
        {
            if (IsMatch(option.Name))
            {
                return true;
            }
        }

        return false;
    }


    bool IsNotIn(CliCommandLine commandLine) => (false == IsIn(commandLine));


    public sealed object? Materialize(CliCommandLine commandLine)
    {
        var captures = commandLine.Options.Where(o => IsMatch(o.Name)).ToList();
        if (OptionType.GetInterfaces().Contains(typeof(IDictionary)))
        {

        }
        else if(IsFlag)
        {
            
        }
        else
        {

            if (captures.Count == 1)
            {
                var scalar = captures.Cast<CliScalarOptionCapture>().Single();
                return ValueConverter.ConvertFrom(scalar.Value);
            }
            if (captures.Count == 0)
            {
                if (IsOptional)
                {
                    return DefaultValue;
                }

                throw new CliExitException($"{Aliases} option is required");
            }
  
        }

        throw new NotImplementedException();
    }



    static Type GetOptionType(Type declaredType, CliOptionAttribute attribute, out TypeConverter? valueConverter)
    {
        var mapSpecs = declaredType.GetGenericDictionaryArgumentTypes().ToList();
        if (mapSpecs.Count == 1)
        {
            var specs = mapSpecs[0];
            if (attribute.CanAccept(specs.Value, out valueConverter))
            {
                return typeof(Dictionary<,>).MakeGenericType([specs.Key, specs.Value]);
            }

            throw new NotImplementedException();
        }

        if (CliFlag.IsFlagType(declaredType, out valueConverter))
        {
            return declaredType;
        }

        if (attribute.CanAccept(declaredType, out valueConverter))
        {
            return declaredType;
        }

        throw new NotImplementedException();
    }
}