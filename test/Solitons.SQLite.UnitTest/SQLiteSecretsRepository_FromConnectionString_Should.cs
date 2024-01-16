// ReSharper disable All
namespace Solitons.SQLite;

public class SQLiteSecretsRepository_FromConnectionString_Should
{
    [Fact]
    public async Task ReturnValidRepository()
    {
        // Arrange
        string validConnectionString = "test.db|scope=testScope";

        // Act
        var repository = SQLiteSecretsRepository
            .FromConnectionString(validConnectionString);

        await repository.SetSecretAsync("test-secret", "test-value");
        var actualValue = await repository.GetSecretAsync("test-secret");
        Assert.Equal("test-value", actualValue);
    }

    [Fact]
    public void UseDefaultScope_WhenScopeIsMissing()
    {
        // Arrange
        string connectionStringWithoutScope = "test.db";

        // Act
        var repository = SQLiteSecretsRepository
            .FromConnectionString(connectionStringWithoutScope);

        // Assert - Add necessary assertions to check if the default scope is used
    }


    [Fact]
    public void ThrowFormatException_ForInvalidFormat()
    {
        // Arrange
        string invalidConnectionString = "invalid_format_string";

        // Act & Assert
        Assert.Throws<FormatException>(() =>
            SQLiteSecretsRepository.FromConnectionString(invalidConnectionString));
    }

    [Fact]
    public void ThrowArgumentException_ForInvalidFilePath()
    {
        // Arrange
        string connectionStringWithInvalidPath = "test.sqlite|scope=testScope";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            SQLiteSecretsRepository.FromConnectionString(connectionStringWithInvalidPath));
    }


}