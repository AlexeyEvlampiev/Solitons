

namespace Solitons.Postgres.PgUp.Management.Models;

public record PgUpTransaction
{
    private readonly PgUpStage[] _stages;

    public PgUpTransaction(PgUpStage[] stages)
    {
        _stages = stages;
    }
    public void Serialize(
        BinaryWriter writer, 
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor)
    {
        var stages = GetStages().ToArray();
        writer.Write(stages.Length);
        foreach (var stage in stages)
        {
            stage.Serialize(writer, workDir, preProcessor);
        }

    }

    public IEnumerable<PgUpStage> GetStages() => _stages;
}