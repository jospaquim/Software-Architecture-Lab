namespace CleanArchitecture.Infrastructure.Secrets;

/// <summary>
/// Interface para proveedores de secrets
/// Permite cambiar fácilmente entre Infisical, Vault, Azure Key Vault, etc.
/// </summary>
public interface ISecretsProvider
{
    /// <summary>
    /// Obtiene todos los secrets del provider
    /// </summary>
    Task<Dictionary<string, string>> GetSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un secret específico por key
    /// </summary>
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);
}
