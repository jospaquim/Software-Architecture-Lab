using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.Secrets;

/// <summary>
/// Cliente para Infisical - Secret Management Platform
/// https://infisical.com
/// </summary>
public class InfisicalClient : ISecretsProvider
{
    private readonly HttpClient _httpClient;
    private readonly InfisicalOptions _options;
    private string? _cachedToken;
    private DateTime _tokenExpiration;

    public InfisicalClient(HttpClient httpClient, InfisicalOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _httpClient.BaseAddress = new Uri(options.ServerUrl);
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v3/secrets/raw?workspaceId={_options.WorkspaceId}&environment={_options.Environment}");
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var secretsResponse = JsonSerializer.Deserialize<InfisicalSecretsResponse>(content);

        return secretsResponse?.Secrets
            .ToDictionary(s => s.SecretKey, s => s.SecretValue)
            ?? new Dictionary<string, string>();
    }

    public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var secrets = await GetSecretsAsync(cancellationToken);
        return secrets.TryGetValue(key, out var value) ? value : null;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Return cached token if still valid
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiration)
        {
            return _cachedToken;
        }

        // Authenticate with service token or client credentials
        var authRequest = new
        {
            clientId = _options.ClientId,
            clientSecret = _options.ClientSecret
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/auth/universal-auth/login",
            authRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<InfisicalAuthResponse>(cancellationToken);

        if (authResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("Failed to authenticate with Infisical");
        }

        _cachedToken = authResponse.AccessToken;
        _tokenExpiration = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn - 60); // 1 min buffer

        return _cachedToken;
    }
}

public class InfisicalOptions
{
    public string ServerUrl { get; set; } = "https://app.infisical.com";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string Environment { get; set; } = "dev";
}

public class InfisicalSecretsResponse
{
    public List<InfisicalSecret> Secrets { get; set; } = new();
}

public class InfisicalSecret
{
    public string SecretKey { get; set; } = string.Empty;
    public string SecretValue { get; set; } = string.Empty;
}

public class InfisicalAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
