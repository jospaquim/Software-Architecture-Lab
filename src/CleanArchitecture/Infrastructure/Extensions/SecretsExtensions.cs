using CleanArchitecture.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure.Extensions;

public static class SecretsExtensions
{
    /// <summary>
    /// Agrega Infisical como proveedor de secrets
    /// </summary>
    public static IConfigurationBuilder AddInfisical(
        this IConfigurationBuilder builder,
        Action<InfisicalOptions> configure,
        bool optional = false)
    {
        var options = new InfisicalOptions();
        configure(options);

        var httpClient = new HttpClient();
        var secretsProvider = new InfisicalClient(httpClient, options);

        return builder.Add(new SecretsConfigurationSource(
            secretsProvider,
            new SecretsConfigurationOptions { Optional = optional }));
    }

    /// <summary>
    /// Agrega Azure Key Vault como proveedor de secrets
    /// </summary>
    public static IConfigurationBuilder AddAzureKeyVault(
        this IConfigurationBuilder builder,
        string keyVaultUrl,
        string? tenantId = null,
        bool optional = false)
    {
        var secretsProvider = new AzureKeyVaultClient(keyVaultUrl, tenantId);

        return builder.Add(new SecretsConfigurationSource(
            secretsProvider,
            new SecretsConfigurationOptions { Optional = optional }));
    }

    /// <summary>
    /// Agrega HashiCorp Vault como proveedor de secrets
    /// </summary>
    public static IConfigurationBuilder AddHashiCorpVault(
        this IConfigurationBuilder builder,
        Action<VaultOptions> configure,
        bool optional = false)
    {
        var options = new VaultOptions();
        configure(options);

        var secretsProvider = new HashiCorpVaultClient(options);

        return builder.Add(new SecretsConfigurationSource(
            secretsProvider,
            new SecretsConfigurationOptions { Optional = optional }));
    }

    /// <summary>
    /// Registra ISecretsProvider en DI
    /// </summary>
    public static IServiceCollection AddSecretsProvider(
        this IServiceCollection services,
        string provider,
        IConfiguration configuration)
    {
        return provider.ToLower() switch
        {
            "infisical" => services.AddInfisicalProvider(configuration),
            "azurekeyvault" or "azure" => services.AddAzureKeyVaultProvider(configuration),
            "vault" or "hashicorp" => services.AddHashiCorpVaultProvider(configuration),
            _ => throw new ArgumentException($"Unknown secrets provider: {provider}")
        };
    }

    private static IServiceCollection AddInfisicalProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Infisical").Get<InfisicalOptions>()
            ?? throw new InvalidOperationException("Infisical configuration not found");

        services.AddHttpClient<ISecretsProvider, InfisicalClient>(client =>
        {
            client.BaseAddress = new Uri(options.ServerUrl);
        });

        services.AddSingleton(options);
        return services;
    }

    private static IServiceCollection AddAzureKeyVaultProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var keyVaultUrl = configuration["AzureKeyVault:VaultUrl"]
            ?? throw new InvalidOperationException("Azure Key Vault URL not configured");
        var tenantId = configuration["AzureKeyVault:TenantId"];

        services.AddSingleton<ISecretsProvider>(sp => new AzureKeyVaultClient(keyVaultUrl, tenantId));
        return services;
    }

    private static IServiceCollection AddHashiCorpVaultProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Vault").Get<VaultOptions>()
            ?? throw new InvalidOperationException("Vault configuration not found");

        services.AddSingleton<ISecretsProvider>(sp => new HashiCorpVaultClient(options));
        return services;
    }
}
