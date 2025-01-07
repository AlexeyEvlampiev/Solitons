namespace Solitons.Postgres.PgUp.Core;

public interface IPgUpTemplateRepository
{
    bool Exists(string? template);

    void Copy(string template, string directory);
}