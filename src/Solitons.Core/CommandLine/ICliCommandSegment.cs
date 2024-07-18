namespace Solitons.CommandLine;

internal interface ICliCommandSegment
{
    string BuildPattern();
    string GetExpressionGroup();
}