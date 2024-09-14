using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed record CliDictionaryOptionInfo : CliOptionInfo
{
    public CliDictionaryOptionInfo(Config config) : base(config)
    {
    }

    public override object Deserialize(Group optionGroup, CliTokenDecoder decoder)
    {
        throw new System.NotImplementedException();
    }

    public static bool IsMatch(Config config, out CliOptionInfo result)
    {
        throw new System.NotImplementedException();
    }
}