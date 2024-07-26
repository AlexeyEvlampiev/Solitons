using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Solitons.CommandLine.Common;

namespace Solitons.CommandLine;

[method: DebuggerStepThrough]
internal sealed class CliBundleOptionInfo(PropertyInfo propertyInfo) 
    : CliOperandInfo(propertyInfo)
{
    private readonly PropertyInfo _propertyInfo = propertyInfo;

    public static implicit operator PropertyInfo(CliBundleOptionInfo optionInfo) => optionInfo._propertyInfo;

    public void SetValues(CliOptionBundle bundle, Match match, TokenSubstitutionPreprocessor preprocessor)
    {
        if (FindValue(match, preprocessor, out var value))
        {
            _propertyInfo.SetValue(bundle, value);
        }
        else if (IsOptional == false)
        {
            throw new InvalidOperationException();
        }
    }

}