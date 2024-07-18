using System;
using System.Diagnostics;
using System.Reflection;

namespace Solitons.CommandLine.ZapCli;

public interface IClParameterTypeConstraint
{
    bool CanAccept(Type parameterType);

    [DebuggerStepThrough]
    public sealed bool CanAccept(ParameterInfo info) => CanAccept(info.ParameterType);
}