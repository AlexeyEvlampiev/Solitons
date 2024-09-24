namespace Solitons.CommandLine;

public interface ICliContext
{
    string GetCommandLine(bool encoded = false);

    string GetProgramName();

    void DisplayHelp(bool useFullMode = false);
}