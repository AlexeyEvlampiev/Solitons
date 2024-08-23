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
    public sealed class Batch : IPgUpBatch
    {
        [JsonPropertyName("workdir")] public string WorkingDirectory { get; set; } = ".";
        [JsonPropertyName("fileDiscoveryMode")] public FileDiscoveryMode FileDiscoveryMode { get; set; }
        [JsonPropertyName("runOrder"), JsonRequired] public string[] ScriptFiles { get; set; } = [];
        [JsonPropertyName("customExec")] public string? CustomExecCommandText{ get; set; }

        [DebuggerHidden]
        IEnumerable<string> IPgUpBatch.GetRunOrder() => ScriptFiles;

        [DebuggerHidden]
        string IPgUpBatch.GetWorkingDirectory() => WorkingDirectory;

        [DebuggerHidden]
        string? IPgUpBatch.GetCustomExecCommandText() => CustomExecCommandText;

        FileDiscoveryMode IPgUpBatch.GetFileDiscoveryMode() => FileDiscoveryMode;
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