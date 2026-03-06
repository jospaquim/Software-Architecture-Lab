using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;

namespace CleanArchitecture.Infrastructure.Secrets;

/// <summary>
/// Cliente para HashiCorp Vault
/// https://www.vaultproject.io/
/// </summary>
public class HashiCorpVaultClient : ISecretsProvider
{
    private readonly IVaultClient _vaultClient;
    private readonly string _secretPath;
    private readonly string _mountPoint;

    public HashiCorpVaultClient(VaultOptions options)
    {
        IAuthMethodInfo authMethod = new TokenAuthMethodInfo(options.Token);

        var vaultClientSettings = new VaultClientSettings(options.Address, authMethod);
        _vaultClient = new VaultClient(vaultClientSettings);

        _secretPath = options.SecretPath;
        _mountPoint = options.MountPoint ?? "secret";
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(CancellationToken cancellationToken = default)
    {
        // Vault KV v2
        var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            path: _secretPath,
            mountPoint: _mountPoint);

        return secret.Data.Data.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty);
    }

    public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var secrets = await GetSecretsAsync(cancellationToken);
        return secrets.TryGetValue(key, out var value) ? value : null;
    }
}

public class VaultOptions
{
    public string Address { get; set; } = "http://localhost:8200";
    public string Token { get; set; } = string.Empty;
    public string SecretPath { get; set; } = "application";
    public string? MountPoint { get; set; } = "secret";
}
