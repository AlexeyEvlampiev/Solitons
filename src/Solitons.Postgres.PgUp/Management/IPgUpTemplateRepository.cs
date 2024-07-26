namespace Solitons.Postgres.PgUp.Management;

public interface IPgUpTemplateRepository
{
    bool Exists(string? template);

    void Copy(string template, string directory);
}