

namespace Solitons.Postgres.PgUp.Management.Models;

public record PgUpTransaction
{
    private readonly PgUpStage[] _stages;

    public PgUpTransaction(PgUpStage[] stages)
    {
        _stages = stages;
    }

    public IEnumerable<PgUpStage> GetStages() => _stages;
}