using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliOption(ParameterInfo parameter) 
    : CliParameter(parameter)
{
}