namespace Solitons.Postgres.PgUp.PgUp;

public interface IPgUpTemplateRepository
{
    bool Exists(string? template);

    void Copy(string template, string directory);
}