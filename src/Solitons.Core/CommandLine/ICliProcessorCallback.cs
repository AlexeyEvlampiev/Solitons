namespace Solitons.CommandLine;

internal interface ICliProcessorCallback
{
    void ShowHelp(
        string commandLine,
        CliTokenDecoder decoder);
}