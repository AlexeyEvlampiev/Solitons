

namespace Solitons.Postgres.PgUp.Models;

public record PgUpTransaction
{
    private readonly PgUpStage[] _stages;

    public PgUpTransaction(string displayName, PgUpStage[] stages)
    {
        DisplayName = displayName.Trim();
        _stages = stages;
    }

    public string DisplayName { get; }

    public IEnumerable<PgUpStage> GetStages() => _stages;
}