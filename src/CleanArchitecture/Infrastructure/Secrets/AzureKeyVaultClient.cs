using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace CleanArchitecture.Infrastructure.Secrets;

/// <summary>
/// Cliente para Azure Key Vault
/// https://azure.microsoft.com/en-us/services/key-vault/
/// </summary>
public class AzureKeyVaultClient : ISecretsProvider
{
    private readonly SecretClient _client;

    public AzureKeyVaultClient(string keyVaultUrl, string? tenantId = null)
    {
        var credential = tenantId != null
            ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId })
            : new DefaultAzureCredential();

        _client = new SecretClient(new Uri(keyVaultUrl), credential);
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(CancellationToken cancellationToken = default)
    {
        var secrets = new Dictionary<string, string>();

        await foreach (var secretProperties in _client.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            if (secretProperties.Enabled == true)
            {
                var secret = await _client.GetSecretAsync(secretProperties.Name, cancellationToken: cancellationToken);
                secrets[secretProperties.Name] = secret.Value.Value;
            }
        }

        return secrets;
    }

    public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = await _client.GetSecretAsync(key, cancellationToken: cancellationToken);
            return secret.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
