namespace Solitons.CommandLine;

interface ICliRouteSegment
{
    string BuildRegularExpression();
    public bool IsArgument => GetType() == typeof(JazzArgumentInfo);
}