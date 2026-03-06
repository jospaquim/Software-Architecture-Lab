using Microsoft.Extensions.Configuration;

namespace CleanArchitecture.Infrastructure.Secrets;

/// <summary>
/// Configuration Provider personalizado que carga secrets desde un ISecretsProvider
/// Se integra con el sistema de configuración de .NET
/// </summary>
public class SecretsConfigurationProvider : ConfigurationProvider
{
    private readonly ISecretsProvider _secretsProvider;
    private readonly SecretsConfigurationOptions _options;

    public SecretsConfigurationProvider(ISecretsProvider secretsProvider, SecretsConfigurationOptions options)
    {
        _secretsProvider = secretsProvider;
        _options = options;
    }

    public override void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        try
        {
            var secrets = await _secretsProvider.GetSecretsAsync();

            foreach (var (key, value) in secrets)
            {
                // Transformar keys según configuración
                var configKey = TransformKey(key);
                Data[configKey] = value;
            }
        }
        catch (Exception ex)
        {
            if (_options.Optional)
            {
                // Log warning pero no fallar
                Console.WriteLine($"Warning: Failed to load secrets from provider: {ex.Message}");
            }
            else
            {
                throw;
            }
        }
    }

    private string TransformKey(string key)
    {
        if (string.IsNullOrEmpty(_options.KeyPrefix))
        {
            return key;
        }

        // Agregar prefix y convertir formato
        // Ejemplo: "JWT_SECRET" -> "Jwt:SecretKey"
        var transformedKey = key;

        if (_options.ReplaceUnderscoreWithColon)
        {
            transformedKey = transformedKey.Replace("_", ":");
        }

        return $"{_options.KeyPrefix}:{transformedKey}";
    }
}

public class SecretsConfigurationOptions
{
    public bool Optional { get; set; } = false;
    public string? KeyPrefix { get; set; }
    public bool ReplaceUnderscoreWithColon { get; set; } = true;
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromMinutes(5);
}

public class SecretsConfigurationSource : IConfigurationSource
{
    private readonly ISecretsProvider _secretsProvider;
    private readonly SecretsConfigurationOptions _options;

    public SecretsConfigurationSource(ISecretsProvider secretsProvider, SecretsConfigurationOptions options)
    {
        _secretsProvider = secretsProvider;
        _options = options;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecretsConfigurationProvider(_secretsProvider, _options);
    }
}
