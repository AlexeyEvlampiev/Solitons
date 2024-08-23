namespace Solitons.CommandLine;

internal interface ICliProcessorCallback
{
    void ShowHelp(
        string executableName,
        string commandLine);
}