using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.ExternalServices;

/// <summary>
/// Example HTTP client with Polly resilience policies
/// Demonstrates how to call external APIs with retry, circuit breaker, timeout
/// </summary>
public interface IExternalApiClient
{
    Task<WeatherForecast?> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
    Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
}

public class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WeatherForecast?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching weather for city: {City}", city);

            // Polly policies are automatically applied to this HttpClient
            var response = await _httpClient.GetAsync(
                $"https://api.weatherapi.com/v1/current.json?q={city}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var weather = JsonSerializer.Deserialize<WeatherForecast>(content);

            _logger.LogInformation("Successfully fetched weather for {City}", city);

            return weather;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching weather for {City}", city);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout while fetching weather for {City}", city);
            throw;
        }
    }

    public async Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching products from external API");

            // Polly policies automatically handle retries, circuit breaker, timeout
            var response = await _httpClient.GetAsync(
                "https://api.example.com/products",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var products = JsonSerializer.Deserialize<List<Product>>(content) ?? new List<Product>();

            _logger.LogInformation("Successfully fetched {Count} products", products.Count);

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from external API");
            // Circuit breaker will open if this fails repeatedly
            throw;
        }
    }
}

// DTOs
public record WeatherForecast(
    string City,
    double Temperature,
    string Condition,
    int Humidity);

public record Product(
    int Id,
    string Name,
    decimal Price,
    string Category);
