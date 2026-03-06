using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CleanArchitecture.IntegrationTests;

public class HealthCheckTests : IClassFixture<WebApplicationFactoryBase<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactoryBase<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthCheckReady_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheckLive_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnValidJsonResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var healthReport = await response.Content.ReadFromJsonAsync<HealthReport>();
        healthReport.Should().NotBeNull();
        healthReport!.Status.Should().Be("Healthy");
    }

    private class HealthReport
    {
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, HealthCheckEntry>? Entries { get; set; }
    }

    private class HealthCheckEntry
    {
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
