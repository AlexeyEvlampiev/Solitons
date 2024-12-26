using System;
using System.Diagnostics;

namespace Solitons.CommandLine;

public abstract class CliGlobalOptionBundle : CliOptionBundle, ICloneable
{
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

}