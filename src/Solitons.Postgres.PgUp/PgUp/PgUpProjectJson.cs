using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Solitons.Data;

namespace Solitons.Postgres.PgUp.PgUp;

[Guid("2654b3b3-7603-453d-a43f-5d288e9491d5")]
public sealed class PgUpProjectJson : BasicJsonDataTransferObject
{
    [JsonPropertyName("parameters")]
    public Dictionary<string, ParameterData> Parameters { get; set; } = new();

    public sealed class ParameterData
    {
        [JsonPropertyName("default")]
        public string Default { get; set; } = String.Empty;
    }


    public void SetDefaultParameterValue(string key, string value)
    {
        Parameters[key] = new ParameterData()
        {
            Default = value
        };
    }
}