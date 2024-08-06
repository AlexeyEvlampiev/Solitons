namespace Solitons.CommandLine;

internal partial class CliHelpRtt
{
    private readonly CliAction[] _actions;

    private CliHelpRtt(CliAction[] actions)
    {
        _actions = actions;
    }

    public static string Build(CliAction[] actions)
    {
        var rtt = new CliHelpRtt(actions);
        return rtt.ToString();
    }
}