// ReSharper disable All
namespace Solitons.SQLite;

public class SQLiteSecretsRepository_ListSecretNamesAsync_Should
{
    private readonly string _filePath = $"{Guid.NewGuid()}.db";


    [Fact]
    public async Task ListAllSecrets()
    {
        // Arrange
        var scopeName = Guid.NewGuid().ToString();
        var repository = SQLiteSecretsRepository.FromFile(_filePath, scopeName);

        // Add some secrets
        await repository.SetSecretAsync("Secret1", "Value1");
        await repository.SetSecretAsync("Secret2", "Value2");
        await repository.SetSecretAsync("Secret3", "Value3");

        // Act
        var secretNames = await repository.ListSecretNamesAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(secretNames);
        Assert.Equal(3, secretNames.Length);
        Assert.Contains("Secret1", secretNames);
        Assert.Contains("Secret2", secretNames);
        Assert.Contains("Secret3", secretNames);
    }

    [Fact]
    public async Task ReturnEmptyArrayWhenNoSecrets()
    {
        // Arrange
        var repository = SQLiteSecretsRepository.FromFile(_filePath, Guid.NewGuid().ToString());

        // Act
        var secretNames = await repository.ListSecretNamesAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(secretNames);
        Assert.Empty(secretNames);
    }

    [Fact]
    public async Task HandleDifferentScopesIndependently()
    {
        // Arrange
        var repository1 = SQLiteSecretsRepository.FromFile(_filePath, "Scope1");
        var repository2 = SQLiteSecretsRepository.FromFile(_filePath, "Scope2");

        // Add secrets to different scopes
        await repository1.SetSecretAsync("Secret1", "Value1");
        await repository2.SetSecretAsync("Secret2", "Value2");

        // Act
        var secretNames1 = await repository1.ListSecretNamesAsync(CancellationToken.None);
        var secretNames2 = await repository2.ListSecretNamesAsync(CancellationToken.None);

        // Assert
        Assert.Contains("Secret1", secretNames1);
        Assert.DoesNotContain("Secret2", secretNames1);
        Assert.Contains("Secret2", secretNames2);
        Assert.DoesNotContain("Secret1", secretNames2);
    }

}