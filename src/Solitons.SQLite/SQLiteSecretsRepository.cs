using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Solitons.Reactive;
using Solitons.Security;
using Solitons.Security.Common;

namespace Solitons.SQLite;

/// <summary>
/// Provides an SQLite-based implementation of a secrets repository.
/// This class allows for storing, retrieving, and managing secrets such as passwords, tokens,
/// or API keys in an SQLite database. It supports basic CRUD operations and scope-based secret management.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SQLiteSecretsRepository : SecretsRepository
{
    /// <summary>
    /// The default scope name for the secrets repository.
    /// </summary>
    /// <remarks>
    /// This constant defines the default scope name used in the secrets repository. A scope in this context 
    /// is a way to segregate or categorize secrets. For instance, different applications or different parts 
    /// of an application might use different scopes to ensure separation of concerns.
    ///
    /// If no scope name is provided when creating an instance of the SQLiteSecretsRepository, 
    /// this default value is used. It helps in managing secrets more effectively by providing 
    /// a basic level of organization without the need for explicit scope specification in simple scenarios.
    ///
    /// The default scope name is "$default", indicating a general or common scope used 
    /// for secrets that do not require a specific categorization.
    /// </remarks>
    public const string DefaultScopeName = "$default";

    private readonly string _connectionString;

    sealed class SecretNotFoundException : KeyNotFoundException { }

    /// <summary>
    /// Creates an instance of the SQLiteSecretsRepository from a file path.
    /// </summary>
    /// <param name="filePath">The file path of the SQLite database.</param>
    /// <param name="scopeName">Optional. The scope name for the secrets. If not provided or whitespace, the default scope name is used.</param>
    /// <returns>An instance of ISecretsRepository configured to use the specified SQLite database file.</returns>
    /// <remarks>
    /// This method constructs a connection string using the provided file path and initializes a new SQLiteSecretsRepository instance with the specified scope.
    /// If the scope name is null or whitespace, the default scope name is used.
    /// </remarks>
    public static ISecretsRepository FromFile(
        string filePath, 
        string? scopeName = null)
    {
        return new SQLiteSecretsRepository(
            $"Data Source={filePath};", 
            scopeName
                .DefaultIfNullOrWhiteSpace(DefaultScopeName)
                .Trim());
    }

    /// <summary>
    /// Creates an instance of the SQLiteSecretsRepository from a given connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to the SQLite database.</param>
    /// <param name="scopeName">Optional. The scope name for the secrets. If not provided or whitespace, the default scope name is used.</param>
    /// <returns>An instance of ISecretsRepository configured to use the SQLite database specified by the connection string.</returns>
    /// <remarks>
    /// This method initializes a new SQLiteSecretsRepository instance with the provided connection string and scope name.
    /// If the scope name is null or whitespace, the default scope name is used.
    /// The method allows for more flexibility by enabling the specification of various connection string parameters.
    /// </remarks>
    public static ISecretsRepository FromConnectionString(
        string connectionString, 
        string? scopeName = null)
    {
        return new SQLiteSecretsRepository(
            connectionString,
            scopeName
                .DefaultIfNullOrWhiteSpace(DefaultScopeName)
                .Trim());
    }


    /// <summary>
    /// Initializes a new instance of the SQLiteSecretsRepository class with the specified file path and scope name.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="scopeName">The scope name for secrets.</param>
    /// <exception cref="ArgumentException">Throws when the provided file path does not end with the .db extension.</exception>
    protected SQLiteSecretsRepository(string connectionString, string scopeName)
    {
        ScopeName = scopeName;

        _connectionString = connectionString;
        using var connection = new SqliteConnection(_connectionString);
        using var command = connection.CreateCommand();
        command.CommandText = $@"

        CREATE TABLE IF NOT EXISTS secret (
            scope VARCHAR(150),
            key VARCHAR(150),
            value TEXT,
            created_utc DATETIME DEFAULT (datetime('now','utc')),
            updated_utc DATETIME DEFAULT (datetime('now','utc')),
            PRIMARY KEY (scope, key)
        );
        CREATE UNIQUE INDEX IF NOT EXISTS idx_secret_scope_key ON secret (scope, key);";
        connection.Open();
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Gets the name of the scope associated with the repository.
    /// </summary>
    /// <value>
    /// The name of the scope used for segregating secrets within the repository.
    /// </value>
    public string ScopeName { get; }

    /// <summary>
    /// Asynchronously retrieves a list of all secret names within the specified scope.
    /// This method queries the SQLite database to fetch the names of all secrets stored under the current scope.
    /// </summary>
    /// <param name="cancellation">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A Task representing the asynchronous operation, resulting in an array of secret names.</returns>
    protected override async Task<string[]> ListSecretNamesAsync(CancellationToken cancellation)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT key FROM secret WHERE scope = @scope;";
        var scopeParameter = command.CreateParameter();
        scopeParameter.ParameterName = "@scope";
        scopeParameter.DbType = DbType.String;
        scopeParameter.Value = ScopeName;
        command.Parameters.Add(scopeParameter);

        await connection.OpenAsync(cancellation);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var list = new List<string>();
        while (await reader.ReadAsync(cancellation))
        {
            list.Add(reader.GetString(0));
        }

        return list.ToArray();
    }

    /// <summary>
    /// Asynchronously gets the value of a secret with the specified name.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="cancellation">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A Task representing the asynchronous operation. The Task's result is the value of the secret.</returns>
    /// <exception cref="SecretNotFoundException">Throws when the secret does not exist.</exception>
    protected override async Task<string> GetSecretAsync(string secretName, CancellationToken cancellation)
    {
        var value = await GetSecretIfExistsAsync(secretName, cancellation);
        return (value.IsPrintable() ? value : throw new SecretNotFoundException())!;
    }

    /// <summary>
    /// Asynchronously gets the value of a secret if it exists.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="cancellation">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A Task representing the asynchronous operation. The Task's result is the value of the secret, or null if the secret does not exist.</returns>
    protected override async Task<string?> GetSecretIfExistsAsync(string secretName, CancellationToken cancellation)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM secret WHERE scope = @scope AND key = @key;";
        var scopeParameter = command.CreateParameter();
        var keyParameter = command.CreateParameter();

        scopeParameter.ParameterName = "@scope";
        scopeParameter.DbType = DbType.String;
        scopeParameter.Value = ScopeName;

        keyParameter.ParameterName = "@key";
        keyParameter.DbType = DbType.String;
        keyParameter.Value = secretName;

        command.Parameters.Add(scopeParameter);
        command.Parameters.Add(keyParameter);

        await connection.OpenAsync(cancellation);
        var result = await command.ExecuteScalarAsync(cancellation);
        if (result is DBNull || result == null)
        {
            return null;
        }

        return result.ToString() ?? "";
    }

    /// <summary>
    /// Asynchronously gets the value of a secret with the specified name. If the secret does not exist, sets the secret with a provided default value.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="defaultValue">The default value to set if the secret does not exist.</param>
    /// <param name="cancellation">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A Task representing the asynchronous operation. The Task's result is the value of the secret, or the default value if the secret did not exist.</returns>
    protected override async Task<string> GetOrSetSecretAsync(string secretName, string defaultValue, CancellationToken cancellation)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        Debug.Assert(transaction is SqliteTransaction);

        // Check if secret already exists
        var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM secret WHERE scope = @scope AND key = @key;";
        var scopeParameter = command.CreateParameter();
        var keyParameter = command.CreateParameter();

        scopeParameter.ParameterName = "@scope";
        scopeParameter.DbType = DbType.String;
        scopeParameter.Value = ScopeName;

        keyParameter.ParameterName = "@key";
        keyParameter.DbType = DbType.String;
        keyParameter.Value = secretName;

        command.Parameters.Add(scopeParameter);
        command.Parameters.Add(keyParameter);

        command.Transaction = (SqliteTransaction)transaction;

        var result = await command.ExecuteScalarAsync(cancellation);
        if (result != null && !(result is DBNull))
        {
            // Secret exists, so return it
            await transaction.CommitAsync(cancellation);
            return result.ToString()!;
        }

        
        // Secret does not exist, so set it
        command.Parameters.Clear();
        command.CommandText = @"
        INSERT INTO secret (scope, key, value, updated_utc) 
        VALUES (@scope, @key, @defaultValue, datetime('now','utc'));";

        var defaultValueParameter = command.CreateParameter();

        defaultValueParameter.ParameterName = "@defaultValue";
        defaultValueParameter.DbType = DbType.String;
        defaultValueParameter.Value = defaultValue;

        command.Parameters.Add(scopeParameter);
        command.Parameters.Add(keyParameter);
        command.Parameters.Add(defaultValueParameter);

        command.Transaction = (SqliteTransaction)transaction;

        await command.ExecuteNonQueryAsync(cancellation);

        await transaction.CommitAsync(cancellation);

        return defaultValue;
    }

    /// <summary>
    /// Asynchronously sets the value of the secret with the specified name.
    /// </summary>
    /// <param name="secretName">The name of the secret to be set.</param>
    /// <param name="secretValue">The value to be set for the specified secret.</param>
    /// <param name="cancellation">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected override async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellation)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
        INSERT OR REPLACE INTO secret (scope, key, value, updated_utc) 
        VALUES (@scope, @key, @value, datetime('now','utc'));";

        var scopeIdParameter = command.CreateParameter();
        var keyParameter = command.CreateParameter();
        var valueParameter = command.CreateParameter();

        scopeIdParameter.ParameterName = "@scope";
        scopeIdParameter.DbType = DbType.String;
        scopeIdParameter.Value = ScopeName;

        keyParameter.ParameterName = "@key";
        keyParameter.DbType = DbType.String;
        keyParameter.Value = secretName;

        valueParameter.ParameterName = "@value";
        valueParameter.DbType = DbType.String;
        valueParameter.Value = secretValue;

        command.Parameters.Add(scopeIdParameter);
        command.Parameters.Add(keyParameter);
        command.Parameters.Add(valueParameter);

        await connection.OpenAsync(cancellation);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    /// <summary>
    /// Checks whether the provided exception is a "secret not found" error.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>
    /// <c>true</c> if the exception is a "secret not found" error; otherwise, <c>false</c>.
    /// </returns>
    protected override bool IsSecretNotFoundError(Exception exception)
    {
        return exception is SecretNotFoundException;
    }


    /// <inheritdoc />
    protected override bool ShouldRetry(RetryPolicyArgs args)
    {
        return args.Exception is DbException {IsTransient: true};
    }
}