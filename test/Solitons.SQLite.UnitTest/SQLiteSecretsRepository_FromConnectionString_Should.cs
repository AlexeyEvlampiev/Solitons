// ReSharper disable All

using Microsoft.Data.Sqlite;

namespace Solitons.SQLite;

public class SQLiteSecretsRepository_FromConnectionString_Should
{
    [Fact]
    public void ReturnValidRepositoryWithValidConnectionString()
    {
        var connectionString = $"Data Source={Guid.NewGuid()}.db;";
        var repository = SQLiteSecretsRepository.FromConnectionString(connectionString);

        Assert.IsType<SQLiteSecretsRepository>(repository);
    }

    [Theory]
    [InlineData("Data Source=:memory:;")]
    [InlineData("Data Source=temp.db;Mode=Memory;Cache=Shared;")]
    public void SupportInMemoryDatabase(string connectionString)
    {
        var repository = SQLiteSecretsRepository.FromConnectionString(connectionString);
        Assert.IsType<SQLiteSecretsRepository>(repository);
    }



    [Fact]
    public void ThrowArgumentExceptionForInvalidConnectionString()
    {
        var invalidConnectionString = "InvalidConnectionString";
        var exception = Assert.Throws<ArgumentException>(() =>
            SQLiteSecretsRepository.FromConnectionString(invalidConnectionString));

    }

    [Fact]
    public void UseDefaultScopeNameIfNotProvided()
    {
        var connectionString = $"Data Source={Guid.NewGuid()}.db;";
        var repository = SQLiteSecretsRepository.FromConnectionString(connectionString) as SQLiteSecretsRepository;

        Assert.Equal(SQLiteSecretsRepository.DefaultScopeName, repository?.ScopeName);
    }

    [Fact]
    public void AcceptCustomScopeName()
    {
        var connectionString = $"Data Source={Guid.NewGuid()}.db;";
        string customScope = "custom-scope";
        var repository = SQLiteSecretsRepository.FromConnectionString(connectionString, customScope) as SQLiteSecretsRepository;

        Assert.Equal(customScope, repository?.ScopeName);
    }

    [Fact]
    public void HandleInvalidScopeNameGracefully()
    {
        var connectionString = $"Data Source={Guid.NewGuid()}.db;";
        string invalidScope = string.Empty; // or use other invalid scope names to test

        var exception = Record.Exception(() =>
            SQLiteSecretsRepository.FromConnectionString(connectionString, invalidScope));

        Assert.Null(exception); // Assuming the implementation gracefully handles invalid scopes
    }
}
