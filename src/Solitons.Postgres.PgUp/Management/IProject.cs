using Solitons.Postgres.PgUp.Management.Models;

namespace Solitons.Postgres.PgUp.Management;

public interface IProject
{
    IEnumerable<PgUpTransaction> GetTransactions(
        DirectoryInfo workDir,
        PgUpScriptPreprocessor preProcessor);
}