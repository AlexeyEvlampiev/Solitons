using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

[method: DebuggerStepThrough]
internal sealed class CliBundleOptionInfo(PropertyInfo propertyInfo) 
    : CliOperandInfo(propertyInfo)
{
    private readonly PropertyInfo _propertyInfo = propertyInfo;

    public static implicit operator PropertyInfo(CliBundleOptionInfo optionInfo) => optionInfo._propertyInfo;

    public void SetValues(CliOptionBundle bundle, Match match, CliTokenDecoder decoder)
    {
        if (FindValue(match, decoder, out var value))
        {
            _propertyInfo.SetValue(bundle, value);
        }
        else if (IsOptional == false)
        {
            throw new InvalidOperationException();
        }
    }

}