namespace Solitons.Postgres.PgUp.Management.Models;

public sealed class PgUpStage(IPgUpStage stage)
{
    private readonly string[] _scriptFiles = stage
        .GetScriptFiles()
        .ToArray();

    private readonly string _workDir = stage.GetWorkingDirectory();

    public void Serialize(
        BinaryWriter writer, 
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor)
    {
        var scriptFiles = GetScriptFiles().ToArray();
        writer.Write(scriptFiles.Length);
        foreach (var scriptFile in scriptFiles)
        {
            var path = scriptFile;
            if (Path.IsPathFullyQualified(scriptFile) == false)
            {
                path = Path.Combine(_workDir, path);
                path = Path.Combine(workDir.FullName, path);
            }

            if (File.Exists(path) == false)
            {
                throw new NotImplementedException();
            }

            var content = File.ReadAllText(path);
            content = preProcessor.Convert(content);
            writer.Write(content);
        }
    }

    public IEnumerable<string> GetScriptFiles() => _scriptFiles;
}