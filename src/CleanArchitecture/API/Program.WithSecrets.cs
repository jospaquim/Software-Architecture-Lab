using CleanArchitecture.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.API;

/// <summary>
/// Ejemplo de configuración con Secrets Management
/// Este archivo muestra cómo configurar diferentes proveedores de secrets
/// </summary>
public class ProgramWithSecrets
{
    public static void Example_Infisical(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // OPCIÓN 1: Cargar secrets en Configuration durante el startup
        builder.Configuration.AddInfisical(options =>
        {
            options.ServerUrl = Environment.GetEnvironmentVariable("INFISICAL_URL") ?? "https://app.infisical.com";
            options.ClientId = Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID") ?? "";
            options.ClientSecret = Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET") ?? "";
            options.WorkspaceId = Environment.GetEnvironmentVariable("INFISICAL_WORKSPACE_ID") ?? "";
            options.Environment = Environment.GetEnvironmentVariable("INFISICAL_ENVIRONMENT") ?? "dev";
        }, optional: false);

        // Ahora todos los secrets están disponibles en Configuration
        // Ejemplo: builder.Configuration["Jwt:SecretKey"]

        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }

    public static void Example_AzureKeyVault(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // OPCIÓN 2: Azure Key Vault (usa Managed Identity en Azure)
        var keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL");
        if (!string.IsNullOrEmpty(keyVaultUrl))
        {
            builder.Configuration.AddAzureKeyVault(
                keyVaultUrl,
                tenantId: Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
                optional: false);
        }

        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }

    public static void Example_HashiCorpVault(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // OPCIÓN 3: HashiCorp Vault
        builder.Configuration.AddHashiCorpVault(options =>
        {
            options.Address = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? "http://localhost:8200";
            options.Token = Environment.GetEnvironmentVariable("VAULT_TOKEN") ?? "";
            options.SecretPath = Environment.GetEnvironmentVariable("VAULT_SECRET_PATH") ?? "application";
            options.MountPoint = "secret";
        }, optional: false);

        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }

    public static void Example_MultipleProviders(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // OPCIÓN 4: Múltiples proveedores (prioridad: último gana)
        // 1. appsettings.json (default)
        // 2. Variables de entorno
        // 3. Secrets del vault

        var secretsProvider = Environment.GetEnvironmentVariable("SECRETS_PROVIDER");

        switch (secretsProvider)
        {
            case "infisical":
                builder.Configuration.AddInfisical(options =>
                {
                    options.ServerUrl = Environment.GetEnvironmentVariable("INFISICAL_URL") ?? "https://app.infisical.com";
                    options.ClientId = Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID") ?? "";
                    options.ClientSecret = Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET") ?? "";
                    options.WorkspaceId = Environment.GetEnvironmentVariable("INFISICAL_WORKSPACE_ID") ?? "";
                    options.Environment = Environment.GetEnvironmentVariable("INFISICAL_ENVIRONMENT") ?? "dev";
                }, optional: true);
                break;

            case "azurekeyvault":
                var keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL");
                if (!string.IsNullOrEmpty(keyVaultUrl))
                {
                    builder.Configuration.AddAzureKeyVault(keyVaultUrl, optional: true);
                }
                break;

            case "vault":
                builder.Configuration.AddHashiCorpVault(options =>
                {
                    options.Address = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? "http://localhost:8200";
                    options.Token = Environment.GetEnvironmentVariable("VAULT_TOKEN") ?? "";
                    options.SecretPath = "application";
                }, optional: true);
                break;
        }

        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }

    // Ejemplo de uso en un servicio
    public class ExampleService
    {
        private readonly IConfiguration _configuration;

        public ExampleService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void UseSecrets()
        {
            // Los secrets se cargan automáticamente desde el vault
            var jwtSecret = _configuration["Jwt:SecretKey"];
            var connectionString = _configuration["ConnectionStrings:DefaultConnection"];
            var apiKey = _configuration["ExternalApi:ApiKey"];

            // Ya no necesitas hardcodear secrets en appsettings.json!
        }
    }
}
