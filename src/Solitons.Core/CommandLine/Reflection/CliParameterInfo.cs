using System.Reflection;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal abstract class CliParameterInfo(ParameterInfo parameter) : ParameterInfoDecorator(parameter)
{
    public abstract object? Materialize(CliCommandLine commandLine);
}