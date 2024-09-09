using System.Reflection;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class JazzArgumentInfo
{
    public JazzArgumentInfo(
        CliRouteArgumentAttribute argument, 
        ParameterInfo parameter)
    {
        
    }

    public object? Deserialize(Match commandlineMatch, CliTokenDecoder decoder)
    {
        throw new System.NotImplementedException();
    }
}