using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Solitons.Data;

namespace Solitons.Postgres.PgUp.Models;


[Guid("2654b3b3-7603-453d-a43f-5d288e9491d5")]
[PgUpVersion("1.0")]
public sealed class PgUpProjectJson : BasicJsonDataTransferObject, IPgUpProject
{
    [JsonPropertyName("parameters")]
    public Dictionary<string, ParameterData> Parameters { get; set; } = new();

    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("databaseOwner")]
    public string DatabaseOwner { get; set; } = string.Empty;

    [JsonPropertyName("transactions")]
    public Transaction[] Transactions { get; set; } = [];

    [DebuggerDisplay("{Default}")]
    public sealed class ParameterData
    {
        [JsonPropertyName("default")]
        public string? Default { get; set; }
    }

    [DebuggerDisplay("Stages: {Stages.Length}")]
    public sealed class Transaction
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        [JsonPropertyName("stages")]
        public Stage[] Stages { get; set; } = [];
    }

    [DebuggerDisplay("{WorkingDirectory}")]
    public sealed class Stage : IPgUpStage
    {
        [JsonPropertyName("workdir")] public string WorkingDirectory { get; set; } = ".";
        [JsonPropertyName("scripts")] public string[] ScriptFiles { get; set; } = [];
        [JsonPropertyName("customExecutor")] public CustomExecutor? CustomExecutor { get; set; }
        IEnumerable<string> IPgUpStage.GetScriptFiles() => ScriptFiles;

        [DebuggerHidden]
        string IPgUpStage.GetWorkingDirectory() => WorkingDirectory;

        public bool HasCustomExecutor(out IPgUpCustomExecutor? customExecutor)
        {
            customExecutor = CustomExecutor;
            return customExecutor != null;
        }
    }

    [DebuggerDisplay("{CommandText}")]
    public sealed class CustomExecutor : IPgUpCustomExecutor
    {
        [JsonPropertyName("filePathParameter"), JsonRequired] public string FilePathParameterName { get; set; } = string.Empty;
        [JsonPropertyName("fileContentParameter"), JsonRequired] public string FileContentParameterName { get; set; } = string.Empty;
        [JsonPropertyName("fileChecksumParameter"), JsonRequired] public string FileChecksumParameterName { get; set; } = string.Empty;
        [JsonPropertyName("command"), JsonRequired] public string CommandText { get; set; } = string.Empty;

        string IPgUpCustomExecutor.GetFilePathParameterName() => FilePathParameterName;

        string IPgUpCustomExecutor.GetFileContentParameterName() => FileContentParameterName;

        string IPgUpCustomExecutor.GetCommandText() => CommandText;
        string IPgUpCustomExecutor.GetFileChecksumParameterName() => FileChecksumParameterName;
    }

    bool IPgUpProject.HasDefaultParameterValue(string key, out string value)
    {
        if (Parameters.TryGetValue(key, out var data) && 
            data.Default != null)
        {
            value = data.Default;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public void SetDefaultParameterValue(string key, string value)
    {
        if (Parameters.TryGetValue(key, out var parameter))
        {
            parameter.Default = value;
        }
        else
        {
            Parameters[key] = new ParameterData()
            {
                Default = value
            };
        }

    }

    public IEnumerable<PgUpTransaction> GetTransactions(
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor)
    {
        foreach (var transaction in Transactions)
        {
            var stages = transaction
                .Stages
                .Select(s => new PgUpStage(s, workDir, preProcessor))
                .ToArray();
            yield return new PgUpTransaction(transaction.DisplayName, stages);
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    IEnumerable<string> IPgUpProject.ParameterNames => Parameters.Keys;
}