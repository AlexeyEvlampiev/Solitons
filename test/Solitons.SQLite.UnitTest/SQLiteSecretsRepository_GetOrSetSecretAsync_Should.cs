
// ReSharper disable All

namespace Solitons.SQLite;

public class SQLiteSecretsRepository_GetOrSetSecretAsync_Should
{
    private readonly string _filePath = $"{Guid.NewGuid()}.db";

    [Fact]
    public async Task GetExistingSecret()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var secretName = "ExistingSecret";
        var secretValue = "SecretValue";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);
        await repository.SetSecretAsync(secretName, secretValue);

        // Act
        var retrievedValue = await repository.GetOrSetSecretAsync(secretName, "DefaultValue");

        // Assert
        Assert.Equal(secretValue, retrievedValue);
    }

    [Fact]
    public async Task SetNewSecretToDefaultValue()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var secretName = "NewSecret";
        var defaultValue = "DefaultValue";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);

        // Act
        var retrievedValue = await repository.GetOrSetSecretAsync(secretName, defaultValue);

        // Assert
        Assert.Equal(defaultValue, retrievedValue);
        // Optionally, verify if the secret was actually set in the repository
        var confirmValue = await repository.GetSecretAsync(secretName);
        Assert.Equal(defaultValue, confirmValue);
    }

    [Fact]
    public async Task HandleInvalidSecretName()
    {
        // Arrange
        var invalidSecretName = ""; // or other invalid name based on your validation logic
        var defaultValue = "DefaultValue";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, Guid.NewGuid().ToString());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetOrSetSecretAsync(invalidSecretName, defaultValue));
    }

}