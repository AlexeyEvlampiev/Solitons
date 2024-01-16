// ReSharper disable All
using Microsoft.Data.Sqlite;

namespace Solitons.SQLite;

public class SQLiteSecretsRepository_FromFile_Should
{
    [Theory]
    [InlineData(null, SQLiteSecretsRepository.DefaultScopeName)]
    [InlineData("test-scope", "test-scope")]
    public async Task ReturnValidRepository(string? scopeNameValue, string expectedScopeName)
    {
        var filePath = $"{Guid.NewGuid()}.db";

        var repository = SQLiteSecretsRepository
            .FromFile(filePath, scopeNameValue);

        Assert.True(repository is SQLiteSecretsRepository target
                    && target.ScopeName.Equals(expectedScopeName, StringComparison.Ordinal));

        await repository.SetSecretAsync("test-secret", "test-value");
        var actualValue = await repository.GetSecretAsync("test-secret");
        Assert.Equal("test-value", actualValue);
    }

    [Fact]
    public void ThrowSqliteExceptionForInvalidFilePath()
    {
        var invalidFilePath = "invalid_path/with_special*chars.db";

        var exception = Assert.Throws<SqliteException>(() =>
            SQLiteSecretsRepository.FromFile(invalidFilePath));
    }

    [Fact]
    public void CreateNewDatabaseFileIfNotExists()
    {
        var filePath = $"{Guid.NewGuid()}.db";
        Assert.False(File.Exists(filePath));

        var repository = SQLiteSecretsRepository.FromFile(filePath);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task HandleWhitespaceScopeNameAsDefault()
    {
        var filePath = $"{Guid.NewGuid()}.db";
        var repository = SQLiteSecretsRepository.FromFile(filePath, "   ");

        Assert.True(repository is SQLiteSecretsRepository target
                    && target.ScopeName.Equals(SQLiteSecretsRepository.DefaultScopeName, StringComparison.Ordinal));
    }

}