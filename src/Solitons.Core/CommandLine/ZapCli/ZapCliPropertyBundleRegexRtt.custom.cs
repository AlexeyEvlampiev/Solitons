using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine.ZapCli;

internal partial class ZapCliPropertyBundleRegexRtt
{
    private readonly IEnumerable<CliOperandInfo> _parameters;

    sealed record Parameter(string Name, string Pattern);


    private ZapCliPropertyBundleRegexRtt(IEnumerable<CliOperandInfo> parameters)
    {
        _parameters = parameters;
    }

    private IEnumerable<Parameter> Parameters => _parameters.Select(pi => new Parameter(pi.Name, pi.OperandKeyPattern));

    public static string From(IEnumerable<CliOperandInfo> operands)
    {
        var rtt = new ZapCliPropertyBundleRegexRtt(operands);
        return rtt.ToString();
    }
}