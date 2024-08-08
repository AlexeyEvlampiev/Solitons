using Solitons.CommandLine;
using Solitons.Postgres.PgUp.Models;
using System.Diagnostics;

namespace Solitons.Postgres.PgUp;

public interface IProject
{
    IEnumerable<PgUpTransaction> GetTransactions(
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor);

    IEnumerable<string> ParameterNames { get; }
    string DatabaseName { get; }
    string DatabaseOwner { get; }
    bool HasDefaultParameterValue(string key, out string value);
    void SetDefaultParameterValue(string parameterKey, string parameterValue);

    public static async Task<IProject> LoadAsync(
        string projectFilePath,  
        Dictionary<string, string> parameters,
        CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        if (false == File.Exists(projectFilePath))
        {
            throw new CliExitException("PgUp project file does not exist.");
        }

        try
        {
            var projectFile = new FileInfo(projectFilePath);
            Trace.WriteLine($"Project file: {projectFile.FullName}");
            var pgUpJson = await File.ReadAllTextAsync(projectFile.FullName, cancellation);
            var project = PgUpSerializer.Deserialize(pgUpJson, parameters);
            return project;
        }
        catch (Exception e)
        {
            throw new CliExitException($"Failed to load project file. {e.Message}");
        }
    }
}