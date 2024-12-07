using Solitons.Reflection;
using System.Reflection;

namespace Solitons.CommandLine.Reflection;

internal class CliOptionBundleParameterInfo(ParameterInfo parameter) : ParameterInfoDecorator(parameter)
{
}