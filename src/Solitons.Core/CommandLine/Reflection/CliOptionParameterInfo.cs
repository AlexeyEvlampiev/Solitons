using System.Reflection;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal sealed class CliOptionParameterInfo(ParameterInfo parameter) : ParameterInfoDecorator(parameter)
{
}