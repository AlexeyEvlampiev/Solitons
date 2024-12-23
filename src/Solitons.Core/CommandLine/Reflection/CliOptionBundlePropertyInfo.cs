using System.Reflection;
using Solitons.Reflection;

namespace Solitons.CommandLine.Reflection;

internal class CliOptionBundlePropertyInfo(PropertyInfo property) : PropertyInfoDecorator(property),  ICliOptionMemberInfo
{
    public bool IsMatch(string optionName)
    {
        throw new System.NotImplementedException();
    }
}