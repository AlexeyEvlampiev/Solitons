

namespace Solitons.Postgres.PgUp.Models;

public record PgUpTransaction
{
    private readonly PgUpStage[] _stages;

    public PgUpTransaction(PgUpStage[] stages)
    {
        _stages = stages;
    }

    public IEnumerable<PgUpStage> GetStages() => _stages;
}