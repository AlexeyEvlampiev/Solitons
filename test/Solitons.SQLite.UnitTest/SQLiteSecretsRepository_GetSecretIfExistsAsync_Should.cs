// ReSharper disable All
namespace Solitons.SQLite;

public class SQLiteSecretsRepository_GetSecretIfExistsAsync_Should
{
    private readonly string _filePath = $"{Guid.NewGuid()}.db";

    [Fact]
    public async Task ReturnCorrectSecretValueIfExists()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var secretName = "ExistingSecret";
        var expectedValue = "SecretValue";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);
        await repository.SetSecretAsync(secretName, expectedValue);

        // Act
        var actualValue = await repository.GetSecretIfExistsAsync(secretName);

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public async Task ReturnNullForNonExistentSecret()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var nonExistentSecret = "NonExistentSecret";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);

        // Act
        var actualValue = await repository.GetSecretIfExistsAsync(nonExistentSecret);

        // Assert
        Assert.Null(actualValue);
    }
}