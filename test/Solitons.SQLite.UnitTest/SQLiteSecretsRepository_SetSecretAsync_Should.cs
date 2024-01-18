// ReSharper disable All
namespace Solitons.SQLite;

public class SQLiteSecretsRepository_SetSecretAsync_Should
{
    private readonly string _filePath = $"{Guid.NewGuid()}.db";

    [Fact]
    public async Task SetNewSecretCorrectly()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var secretName = "NewSecret";
        var secretValue = "NewValue";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);

        // Act
        await repository.SetSecretAsync(secretName, secretValue);
        var retrievedValue = await repository.GetSecretAsync(secretName);

        // Assert
        Assert.Equal(secretValue, retrievedValue);
    }

    [Fact]
    public async Task UpdateExistingSecretCorrectly()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var secretName = "ExistingSecret";
        var initialValue = "InitialValue";
        var updatedValue = "UpdatedValue";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);
        await repository.SetSecretAsync(secretName, initialValue);

        // Act
        await repository.SetSecretAsync(secretName, updatedValue);
        var retrievedValue = await repository.GetSecretAsync(secretName);

        // Assert
        Assert.Equal(updatedValue, retrievedValue);
    }

    [Fact]
    public async Task ThrowExceptionForInvalidSecretName()
    {
        // Arrange
        var invalidSecretName = ""; // or other invalid name based on your validation logic
        var secretValue = "Value";
        var repository = SQLiteSecretsRepository.FromFile(_filePath, Guid.NewGuid().ToString());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => repository.SetSecretAsync(invalidSecretName, secretValue));
    }

}