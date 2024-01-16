// ReSharper disable All
namespace Solitons.SQLite.Tests;

public class SQLiteSecretsRepository_GetSecretAsync_Should
{
    private readonly string _filePath = $"{Guid.NewGuid()}.db";

    [Fact]
    public async Task RetrieveCorrectSecretValue()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var secretName = "TestSecret";
        var expectedValue = "TestValue";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);
        await repository.SetSecretAsync(secretName, expectedValue);

        // Act
        var actualValue = await repository.GetSecretAsync(secretName);

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public async Task ThrowSecretNotFoundExceptionForMissingSecret()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var secretName = "NonExistentSecret";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);

        try
        {
            await repository.GetSecretAsync(secretName);
            Assert.Fail("Expected KeyNotFoundException");
        }
        catch (Exception e)
        {
            Assert.True(repository.IsSecretNotFoundError(e));
        }

    }

}