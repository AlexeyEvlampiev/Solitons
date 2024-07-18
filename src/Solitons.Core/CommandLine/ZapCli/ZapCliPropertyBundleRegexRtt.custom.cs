using System.Collections.Generic;
using System.Linq;

namespace Solitons.CommandLine.ZapCli;

internal partial class ZapCliPropertyBundleRegexRtt
{
    private readonly IEnumerable<CliOperand> _parameters;

    sealed record Parameter(string Name, string Pattern);


    private ZapCliPropertyBundleRegexRtt(IEnumerable<CliOperand> parameters)
    {
        _parameters = parameters;
    }

    private IEnumerable<Parameter> Parameters => _parameters.Select(pi => new Parameter(pi.Name, pi.RegularExpression));

    public static string From(IEnumerable<CliOperand> operands)
    {
        var rtt = new ZapCliPropertyBundleRegexRtt(operands);
        return rtt.ToString();
    }
}