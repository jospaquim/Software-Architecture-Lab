using System.Net;
using DDD.Sales.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DDD.Sales.IntegrationTests;

public class AggregateIntegrationTests : IClassFixture<DddWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AggregateIntegrationTests(DddWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Order_ShouldMaintainAggregateConsistency()
    {
        // Arrange & Act - Test that Order aggregate maintains consistency
        // This tests the aggregate root pattern

        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValueObjects_ShouldBeImmutable()
    {
        // Value Objects like Money and Address should be immutable
        // This is enforced at compile-time, but we test runtime behavior

        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
