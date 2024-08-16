namespace Solitons.Postgres.PgUp;

public record PgUpTransaction
{
    private readonly PgUpBatch[] _stages;

    public PgUpTransaction(string displayName, PgUpBatch[] stages)
    {
        DisplayName = displayName.Trim();
        _stages = stages;
    }

    public string DisplayName { get; }

    public IEnumerable<PgUpBatch> GetBatches() => _stages;
}