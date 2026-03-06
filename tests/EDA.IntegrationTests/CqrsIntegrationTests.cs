using System.Net;
using EDA.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace EDA.IntegrationTests;

public class CqrsIntegrationTests : IClassFixture<EdaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CqrsIntegrationTests(EdaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Commands_ShouldWriteToEventStore()
    {
        // CQRS: Commands modify state by storing events

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", new { CustomerId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Queries_ShouldReadFromReadModel()
    {
        // CQRS: Queries read from optimized read models

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WriteAndRead_ShouldBeEventuallyConsistent()
    {
        // CQRS: Write model (events) and read model (projections) are eventually consistent

        // Arrange & Act - Write
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new { CustomerId = Guid.NewGuid() });
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Read immediately (should be consistent in this in-memory implementation)
        var readResponse = await _client.GetAsync($"/api/orders/{orderId}");

        // Assert
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
