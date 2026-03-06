using EDA.Application.EventHandlers;
using EDA.Domain.Events;
using EDA.Domain.ReadModel;
using EDA.Infrastructure.ReadModel;
using FluentAssertions;
using Moq;
using Xunit;

namespace EDA.Tests.EventHandlers;

public class OrderCreatedEventHandlerTests
{
    private readonly Mock<IReadModelRepository> _readModelRepositoryMock;
    private readonly OrderCreatedEventHandler _handler;

    public OrderCreatedEventHandlerTests()
    {
        _readModelRepositoryMock = new Mock<IReadModelRepository>();
        _handler = new OrderCreatedEventHandler(_readModelRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithOrderCreatedEvent_ShouldCreateOrderReadModel()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var @event = new OrderCreatedEvent(orderId, customerId)
        {
            Version = 1,
            Timestamp = DateTime.UtcNow
        };

        _readModelRepositoryMock
            .Setup(x => x.SaveOrderAsync(It.IsAny<OrderReadModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _readModelRepositoryMock.Verify(x => x.SaveOrderAsync(
            It.Is<OrderReadModel>(o =>
                o.OrderId == orderId &&
                o.CustomerId == customerId &&
                o.Status == "Created" &&
                o.Items.Count == 0 &&
                o.TotalAmount == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleEvents_ShouldCreateMultipleReadModels()
    {
        // Arrange
        var event1 = new OrderCreatedEvent(Guid.NewGuid(), Guid.NewGuid())
        {
            Version = 1,
            Timestamp = DateTime.UtcNow
        };

        var event2 = new OrderCreatedEvent(Guid.NewGuid(), Guid.NewGuid())
        {
            Version = 1,
            Timestamp = DateTime.UtcNow
        };

        _readModelRepositoryMock
            .Setup(x => x.SaveOrderAsync(It.IsAny<OrderReadModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(event1, CancellationToken.None);
        await _handler.Handle(event2, CancellationToken.None);

        // Assert
        _readModelRepositoryMock.Verify(x => x.SaveOrderAsync(
            It.IsAny<OrderReadModel>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenRepositoryFails_ShouldPropagateException()
    {
        // Arrange
        var @event = new OrderCreatedEvent(Guid.NewGuid(), Guid.NewGuid())
        {
            Version = 1,
            Timestamp = DateTime.UtcNow
        };

        _readModelRepositoryMock
            .Setup(x => x.SaveOrderAsync(It.IsAny<OrderReadModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(@event, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task Handle_ShouldSetTimestampFromEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var @event = new OrderCreatedEvent(orderId, customerId)
        {
            Version = 1,
            Timestamp = timestamp
        };

        _readModelRepositoryMock
            .Setup(x => x.SaveOrderAsync(It.IsAny<OrderReadModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _readModelRepositoryMock.Verify(x => x.SaveOrderAsync(
            It.Is<OrderReadModel>(o => o.CreatedAt == timestamp),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
