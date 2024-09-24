using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Solitons.CommandLine;

internal sealed class CliContext : ICliContext
{
    private readonly string _commandLine;

    public CliContext(
        string commandLine)
    {
        _commandLine = commandLine;
        EncodedCommandLine = CliTokenEncoder
            .Encode(commandLine, out var decoder)
            .Trim();
        Decoder = decoder;

        var programNameMatch = Regex.Match(EncodedCommandLine, @"^\S+");
        Debug.Assert(programNameMatch.Success);

        var programName = Regex.Replace(
            commandLine,
            @"(?xis-m)^\S+",
            m =>
            {
                var filePath = decoder(m.Value);
                var fileName = Path.GetFileName(filePath);
                return fileName;
            });
        ProgramName = Path.GetFileName(decoder(programNameMatch.Value));

        IsEmpty = Regex.IsMatch(EncodedCommandLine, @"^\S*$");
        IsCommandListRequest = CliHelpOptionAttribute.IsGeneralHelpRequest(commandLine);
    }

    public string ProgramName { get; }

    public CliTokenDecoder Decoder { get; }

    public string EncodedCommandLine { get; }
    public bool IsEmpty { get; }
    public bool IsCommandListRequest { get; }


    public string GetCommandLine(bool encoded = false)
    {
        throw new System.NotImplementedException();
    }

    public string GetProgramName()
    {
        throw new System.NotImplementedException();
    }

    public void DisplayHelp(bool useFullMode = false)
    {
        throw new System.NotImplementedException();
    }

    public override string ToString() => _commandLine;

}