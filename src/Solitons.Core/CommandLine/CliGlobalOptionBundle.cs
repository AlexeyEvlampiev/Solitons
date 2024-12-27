using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Solitons.Collections;
using Solitons.CommandLine.Reflection;

namespace Solitons.CommandLine;

public abstract class CliGlobalOptionBundle : CliOptionBundle, ICloneable
{
    private readonly ImmutableArray<CliOptionBundlePropertyInfo> _options;


    public CliGlobalOptionBundle()
    {
        _options = 
            [
                ..GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .SelectMany(pi =>
                    {
                        var attributes = pi.GetCustomAttributes(true);
                        if (attributes.OfType<CliOptionAttribute>().Any())
                        {
                            return FluentEnumerable.Yield(new CliOptionBundlePropertyInfo(pi));
                        }

                        return [];
                    })
            ];
    }


    public new static bool IsAssignableFrom(Type type) => typeof(CliGlobalOptionBundle).IsAssignableFrom(type);

    public virtual void OnExecutingAction(CliCommandLine commandLine)
    {
        Debug.WriteLine($"{GetType()}.{nameof(OnExecutingAction)}");
    }

    public virtual void OnActionExecuted(CliCommandLine commandLine)
    {
        Debug.WriteLine($"{GetType()}.{nameof(OnActionExecuted)}");
    }

    public virtual void OnError(CliCommandLine commandLine, Exception exception)
    {
        Debug.WriteLine($"{GetType()}.{nameof(OnError)}");
    }

    public CliGlobalOptionBundle Clone() => (CliGlobalOptionBundle)this.MemberwiseClone();

    object ICloneable.Clone() => Clone();

    public IEnumerable<ICliOptionMemberInfo> GetOptions()
    {
        foreach (var option in _options)
        {
            yield return option;
        }
    }
}