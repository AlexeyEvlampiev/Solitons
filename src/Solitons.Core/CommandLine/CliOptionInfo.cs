using System.Reflection;

namespace Solitons.CommandLine;

internal sealed class CliOptionInfo(ParameterInfo parameter) 
    : CliParameterInfo(parameter)
{
}