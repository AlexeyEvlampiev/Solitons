using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Solitons.Management.Azure;
using Solitons.Security;
using Solitons.Security.Common;

namespace Solitons.Azure.KeyVault;

/// <summary>
/// Represents a repository for managing secrets stored in Azure Key Vault.
/// </summary>
/// <remarks>
/// This repository provides functionality for creating instances, 
/// managing secrets, and handling exceptions specific to Azure Key Vault operations.
/// </remarks>
public sealed class KeyVaultSecretsRepository : SecretsRepository, ISecretsRepository
{
    private readonly SecretClient _nativeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultSecretsRepository"/> class using a <see cref="SecretClient"/>.
    /// </summary>
    /// <param name="nativeClient">The Azure Key Vault SecretClient.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nativeClient"/> is null.</exception>
    private KeyVaultSecretsRepository(SecretClient nativeClient)
    {
        _nativeClient = nativeClient ?? throw new ArgumentNullException(nameof(nativeClient));
    }


    /// <summary>
    /// Creates a repository instance using the provided Azure Key Vault SecretClient.
    /// </summary>
    /// <param name="nativeClient">The Azure Key Vault SecretClient.</param>
    /// <returns>A new instance of <see cref="KeyVaultSecretsRepository"/>.</returns>
    [DebuggerNonUserCode]
    public static ISecretsRepository Create(SecretClient nativeClient) => new KeyVaultSecretsRepository(nativeClient);

    private KeyVaultSecretsRepository(Uri vaultUri, TokenCredential credential)
    {
        if (vaultUri == null) throw new ArgumentNullException(nameof(vaultUri));
        if (credential == null) throw new ArgumentNullException(nameof(credential));
        _nativeClient = new SecretClient(vaultUri, credential);
    }

    private KeyVaultSecretsRepository(string keyVaultUrl, string tenantId, string clientId, string clientSecret)
    {
        if (false == Uri.IsWellFormedUriString(keyVaultUrl, UriKind.Absolute))
            throw new ArgumentException($"'{keyVaultUrl}' is not a valid uri.");
        _nativeClient = new SecretClient(vaultUri:
            new Uri(keyVaultUrl), credential: new ClientSecretCredential(tenantId, clientId, clientSecret));
    }


    /// <summary>
    /// Creates an instance of the <see cref="KeyVaultSecretsRepository"/> using Azure Key Vault URI and client credentials.
    /// </summary>
    /// <remarks>
    /// This static method initializes the <see cref="KeyVaultSecretsRepository"/> with a specific Azure Key Vault URI and 
    /// client credentials, allowing access to the secrets within the specified Key Vault.
    /// </remarks>
    /// <param name="uri">The URI of the Azure Key Vault.</param>
    /// <param name="tenantId">The tenant ID in Azure Active Directory associated with the Key Vault.</param>
    /// <param name="clientId">The client ID of an Azure Active Directory application with access to the Key Vault.</param>
    /// <param name="secret">The client secret for the Azure Active Directory application.</param>
    /// <returns>An instance of <see cref="KeyVaultSecretsRepository"/> configured to access the specified Azure Key Vault.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="uri"/> is not a well-formed URI.</exception>
    /// <exception cref="ArgumentNullException">Thrown if any of the parameters are null.</exception>
    [DebuggerStepThrough]
    public static ISecretsRepository Create(
        Uri uri,
        string tenantId,
        string clientId,
        string secret)
    {
        return new KeyVaultSecretsRepository(
            uri.ToString(),
            tenantId,
            clientId,
            secret);
    }

    /// <summary>
    /// Creates an instance of the <see cref="KeyVaultSecretsRepository"/> using Azure Key Vault URI and Azure client secret credentials.
    /// </summary>
    /// <remarks>
    /// This method provides a convenient way to instantiate a <see cref="KeyVaultSecretsRepository"/>
    /// using an Azure Key Vault URI and a custom credentials object that encapsulates tenant ID, 
    /// client ID, and client secret. It is particularly useful when working with abstractions 
    /// or higher-level credential management.
    /// </remarks>
    /// <param name="uri">The URI of the Azure Key Vault.</param>
    /// <param name="credentials">An instance of <see cref="IAzureClientSecretCredentials"/> containing the Azure client credentials.</param>
    /// <returns>An instance of <see cref="KeyVaultSecretsRepository"/> configured to access the specified Azure Key Vault.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="uri"/> or <paramref name="credentials"/> is null.</exception>
    [DebuggerStepThrough]
    public static ISecretsRepository Create(
        Uri uri,
        IAzureClientSecretCredentials credentials)
    {
        return new KeyVaultSecretsRepository(
            uri.ToString(),
            credentials.TenantId,
            credentials.ClientId,
            credentials.ClientSecret);
    }

    /// <summary>
    /// Lists all secret names asynchronously from the Azure Key Vault.
    /// </summary>
    /// <param name="cancellation">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing an array of secret names.</returns>
    protected override async Task<string[]> ListSecretNamesAsync(CancellationToken cancellation)
    {
        return await Observable
            .FromAsync(async () =>
            {
                var names = new List<string>();
                var properties = _nativeClient
                    .GetPropertiesOfSecretsAsync(cancellation);
                await foreach (var property in properties)
                {
                    names.Add(property.Name);
                }

                return names.ToArray();
            })
            .WithRetryPolicy(args => args
                .SignalNextAttempt(args.AttemptNumber < 5)
                .Delay(attempt => TimeSpan
                    .FromMilliseconds(50)
                    .ScaleByFactor(1.1, attempt)))
            .ToTask(cancellation);
    }

    /// <summary>
    /// Asynchronously retrieves the value of a secret from the Azure Key Vault.
    /// </summary>
    /// <remarks>
    /// This method performs an asynchronous operation to fetch the secret value associated with 
    /// the specified <paramref name="secretName"/> from the Azure Key Vault. If the secret does 
    /// not exist or is inaccessible, the method will throw an exception.
    /// </remarks>
    /// <param name="secretName">The name of the secret to retrieve from the Key Vault.</param>
    /// <param name="cancellation">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, which, upon completion, will return the value of the specified secret.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="secretName"/> is null or empty.</exception>
    /// <exception cref="RequestFailedException">Thrown if there is an issue with the Key Vault service request, such as a failure in retrieving the secret.</exception>
    protected override async Task<string> GetSecretAsync(string secretName, CancellationToken cancellation)
    {
        var secret = await _nativeClient.GetSecretAsync(secretName, cancellationToken: cancellation);
        return secret.Value.Value;
    }

    /// <summary>
    /// Asynchronously retrieves the value of a secret from the Azure Key Vault, if it exists.
    /// </summary>
    /// <remarks>
    /// This method performs an asynchronous operation to fetch the value of the secret associated
    /// with the provided <paramref name="secretName"/>. If the secret is not found, instead of 
    /// throwing an exception, it returns null. This method is useful for scenarios where the 
    /// existence of a secret is uncertain, and you wish to avoid exception handling for 
    /// non-existent secrets.
    /// </remarks>
    /// <param name="secretName">The name of the secret to retrieve from the Key Vault.</param>
    /// <param name="cancellation">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, which, upon completion, will return the value of the secret if it exists; 
    /// otherwise, null.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="secretName"/> is null or empty.</exception>
    /// <exception cref="RequestFailedException">Thrown if there is an issue with the Key Vault service request, 
    /// such as a failure in retrieving the secret due to reasons other than its non-existence.</exception>
    protected override async Task<string?> GetSecretIfExistsAsync(string secretName, CancellationToken cancellation)
    {
        try
        {
            var response = await Observable
                .FromAsync(() => _nativeClient
                    .GetSecretAsync(secretName, cancellationToken: cancellation))
                .Select(r => r.Value)
                .WithRetryPolicy(args => args
                    .SignalNextAttempt(args.AttemptNumber < 5)
                    .Delay(attempt => TimeSpan
                        .FromMilliseconds(50)
                        .ScaleByFactor(1.1, attempt)))
                .ToTask(cancellation);

            return response.Value;
        }
        catch (Exception ex) when (IsSecretNotFoundError(ex))
        {
            return null;
        }
    }

    /// <summary>
    /// Asynchronously retrieves the value of a specified secret from the Azure Key Vault, or sets it to a default value if the secret does not exist.
    /// </summary>
    /// <remarks>
    /// This method first attempts to fetch the secret associated with the given <paramref name="secretName"/>.
    /// If the secret does not exist in the Key Vault, it sets the secret to the provided <paramref name="defaultValue"/>.
    /// This is useful for scenarios where you want to ensure a secret exists and has a value, 
    /// setting a default if it does not already exist.
    /// </remarks>
    /// <param name="secretName">The name of the secret to retrieve or set in the Key Vault.</param>
    /// <param name="defaultValue">The default value to set for the secret if it does not exist.</param>
    /// <param name="cancellation">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, which, upon completion, will return the existing or newly set value of the secret.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="secretName"/> or <paramref name="defaultValue"/> is null or empty.</exception>
    /// <exception cref="RequestFailedException">Thrown if there is an issue with the Key Vault service request, such as a failure in retrieving or setting the secret.</exception>
    protected override async Task<string> GetOrSetSecretAsync(string secretName, string defaultValue, CancellationToken cancellation)
    {
        try
        {
            var bundle = await _nativeClient.GetSecretAsync(secretName, cancellationToken: cancellation);
            return bundle.Value.Value;
        }
        catch (Exception ex) when (IsSecretNotFoundError(ex))
        {
            var bundle = await _nativeClient.SetSecretAsync(secretName, defaultValue, cancellation);
            return bundle.Value.Value;
        }
    }

    /// <summary>
    /// Asynchronously sets the value of a secret in the Azure Key Vault.
    /// </summary>
    /// <remarks>
    /// This method performs an asynchronous operation to set the value of a secret identified by 
    /// <paramref name="secretName"/> in the Azure Key Vault. If the secret already exists, its value 
    /// will be updated with <paramref name="secretValue"/>.
    /// </remarks>
    /// <param name="secretName">The name of the secret to set in the Key Vault.</param>
    /// <param name="secretValue">The value to set for the secret.</param>
    /// <param name="cancellation">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation of setting the secret.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="secretName"/> or <paramref name="secretValue"/> is null or empty.</exception>
    /// <exception cref="RequestFailedException">Thrown if there is an issue with the Key Vault service request, such as a failure in setting the secret.</exception>
    protected override Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellation)
    {
        return _nativeClient.SetSecretAsync(secretName, secretValue, cancellation);
    }

    /// <summary>
    /// Determines whether the specified exception is due to a missing secret in the Azure Key Vault.
    /// </summary>
    /// <remarks>
    /// This method checks if the given <paramref name="exception"/> is specifically a 
    /// <see cref="RequestFailedException"/> indicating that a secret was not found in the Key Vault. 
    /// This can be useful for differentiating between different types of failures when accessing secrets.
    /// </remarks>
    /// <param name="exception">The exception to check.</param>
    /// <returns>
    /// <c>true</c> if the exception is a <see cref="RequestFailedException"/> caused by a missing secret; otherwise, <c>false</c>.
    /// </returns>
    protected override bool IsSecretNotFoundError(Exception exception)
    {
        if (exception is RequestFailedException ex)
        {
            return ex.Status == (int)HttpStatusCode.NotFound;
        }

        return false;
    }

    /// <summary>
    /// Gets the string representation of the Key Vault's URI.
    /// </summary>
    /// <returns>The URI of the Azure Key Vault as a string.</returns>
    public override string ToString() => _nativeClient.VaultUri.ToString();

    /// <summary>
    /// Determines whether the specified URI string is a well-formed URI for an Azure Key Vault.
    /// </summary>
    /// <remarks>
    /// This method checks if the provided <paramref name="uri"/> string is not only a well-formed absolute URI 
    /// but also conforms to the pattern of Azure Key Vault URLs. This is particularly useful for validating URIs 
    /// before attempting to connect to Azure Key Vault resources.
    /// </remarks>
    /// <param name="uri">The URI string to validate.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="uri"/> is a well-formed absolute URI and matches the Azure Key Vault URL pattern; 
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidKeyVaultUri(string uri)
    {
        return Uri.IsWellFormedUriString(uri, UriKind.Absolute) &&
               Regex.IsMatch(uri, @"$https://(?:\w-_)+.vault.azure.net/");
    }
}