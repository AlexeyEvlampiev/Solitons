using System;
using System.Diagnostics;

namespace Solitons.CommandLine.ZapCli;

public class CliMasterOptionBundle : CliOptionBundle
{
    public new static bool IsAssignableFrom(Type type) => typeof(CliMasterOptionBundle).IsAssignableFrom(type);

    public virtual void OnExecutingAction(string commandLine)
    {
        Debug.WriteLine($"{GetType()}.{nameof(OnExecutingAction)}");
    }

    public virtual void OnActionExecuted(string commandLine)
    {
        Debug.WriteLine($"{GetType()}.{nameof(OnActionExecuted)}");
    }

    public virtual void OnError(string commandLine, Exception exception)
    {
        Debug.WriteLine($"{GetType()}.{nameof(OnError)}");
    }
}