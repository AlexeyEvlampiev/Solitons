using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Solitons.Data;

namespace Solitons.Postgres.PgUp.Management.Models;

[Guid("2654b3b3-7603-453d-a43f-5d288e9491d5")]
public sealed class PgUpProjectJson : BasicJsonDataTransferObject, IProject
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
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        [JsonPropertyName("stages")]
        public Stage[] Stages { get; set; } = [];

    }

    [DebuggerDisplay("{WorkingDirectory}")]
    public sealed class Stage : IPgUpStage
    {
        [JsonPropertyName("workdir")] public string WorkingDirectory { get; set; } = ".";
        [JsonPropertyName("scripts")] public string[] ScriptFiles { get; set; } = [];
        IEnumerable<string> IPgUpStage.GetScriptFiles() => ScriptFiles;
        string IPgUpStage.GetWorkingDirectory() => WorkingDirectory;
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

    public IEnumerable<PgUpTransaction> GetTransactions()
    {
        foreach (var transaction in Transactions)
        {
            var stages = transaction
                .Stages
                .Select(s => new PgUpStage(s))
                .ToArray();
            yield return new PgUpTransaction(stages);
        }
    }
}