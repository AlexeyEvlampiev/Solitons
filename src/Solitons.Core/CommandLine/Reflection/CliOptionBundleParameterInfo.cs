using System.Reflection;

namespace Solitons.CommandLine.Reflection;

internal class CliOptionBundleParameterInfo(ParameterInfo parameter) : CliParameterInfo(parameter)
{
    public override object Parse(string arg)
    {
        throw new System.NotImplementedException();
    }
}