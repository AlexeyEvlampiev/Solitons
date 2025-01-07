namespace Solitons.Postgres.PgUp.Core;

public interface IPgUpSession
{
    Task TestConnectionAsync(string connectionString);

    Task DropDatabaseIfExistsAsync(
        string connectionString,
        string databaseName);

    Task ProvisionDatabaseAsync(
        string connectionString,
        string databaseName,
        string databaseOwner);

    Task ExecuteAsync(
        PgUpTransaction pgUpTransaction,
        string connectionString);
}