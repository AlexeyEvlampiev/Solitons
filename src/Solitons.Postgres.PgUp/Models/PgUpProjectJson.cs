using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Solitons.Data;

namespace Solitons.Postgres.PgUp.Models;


[Guid("2654b3b3-7603-453d-a43f-5d288e9491d5")]
[PgUpVersion("1.0")]
public sealed class PgUpProjectJson : BasicJsonDataTransferObject, IPgUpProject
{
    [JsonPropertyName("parameters"), JsonRequired]
    public Dictionary<string, ParameterData> Parameters { get; set; } = new();

    [JsonPropertyName("databaseName"), JsonRequired]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("databaseOwner"), JsonRequired]
    public string DatabaseOwner { get; set; } = string.Empty;

    [JsonPropertyName("transactions"), JsonRequired]
    public Transaction[] Transactions { get; set; } = [];

    [DebuggerDisplay("{Default}")]
    public sealed class ParameterData
    {
        [JsonPropertyName("default")]
        public string? Default { get; set; }
    }

    [DebuggerDisplay("Batches: {Batches.Length}")]
    public sealed class Transaction
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        [JsonPropertyName("batches"), JsonRequired]
        public Batch[] Batches { get; set; } = [];
    }

    [DebuggerDisplay("{WorkingDirectory}")]
    public sealed class Batch : IPgUpStage
    {
        [JsonPropertyName("workdir")] public string WorkingDirectory { get; set; } = ".";
        [JsonPropertyName("scripts"), JsonRequired] public string[] ScriptFiles { get; set; } = [];
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
        [JsonPropertyName("parameters"), JsonRequired] public CustomExecutorParameters Paraneters { get; set; }
        [JsonPropertyName("command"), JsonRequired] public string CommandText { get; set; } = string.Empty;

        string IPgUpCustomExecutor.GetFilePathParameterName() => Paraneters.FilePathParameterName;

        string IPgUpCustomExecutor.GetFileContentParameterName() => Paraneters.FileContentParameterName;

        string IPgUpCustomExecutor.GetCommandText() => CommandText;
        string IPgUpCustomExecutor.GetFileChecksumParameterName() => Paraneters.FileChecksumParameterName;
    }

    public sealed class CustomExecutorParameters
    {
        [JsonPropertyName("path"), JsonRequired] public string FilePathParameterName { get; set; } = string.Empty;
        [JsonPropertyName("content"), JsonRequired] public string FileContentParameterName { get; set; } = string.Empty;
        [JsonPropertyName("checksum"), JsonRequired] public string FileChecksumParameterName { get; set; } = string.Empty;
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
                .Batches
                .Select(s => new PgUpBatch(s, workDir, preProcessor))
                .ToArray();
            yield return new PgUpTransaction(transaction.DisplayName, stages);
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    IEnumerable<string> IPgUpProject.ParameterNames => Parameters.Keys;
}