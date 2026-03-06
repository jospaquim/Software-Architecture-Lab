using EDA.Application.Commands;
using EDA.Domain.WriteModel;
using EDA.Infrastructure.EventStore;
using FluentAssertions;
using Moq;
using Xunit;

namespace EDA.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _eventStoreMock = new Mock<IEventStore>();
        _handler = new CreateOrderCommandHandler(_eventStoreMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldStoreOrderCreatedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId
        };

        _eventStoreMock
            .Setup(x => x.SaveEventsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<IEvent>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        _eventStoreMock.Verify(x => x.SaveEventsAsync(
            It.IsAny<Guid>(),
            It.Is<IEnumerable<IEvent>>(events =>
                events.Count() == 1 &&
                events.First().GetType().Name == "OrderCreatedEvent"),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyCustomerId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.Empty
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Customer ID is required");

        _eventStoreMock.Verify(x => x.SaveEventsAsync(
            It.IsAny<Guid>(),
            It.IsAny<IEnumerable<IEvent>>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEventStoreFails_ShouldReturnFailure()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId
        };

        _eventStoreMock
            .Setup(x => x.SaveEventsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<IEvent>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Event store connection failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Event store connection failed");
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueOrderId()
    {
        // Arrange
        var command1 = new CreateOrderCommand { CustomerId = Guid.NewGuid() };
        var command2 = new CreateOrderCommand { CustomerId = Guid.NewGuid() };

        _eventStoreMock
            .Setup(x => x.SaveEventsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<IEvent>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        result1.Data.Should().NotBe(result2.Data);
        result1.Data.Should().NotBeEmpty();
        result2.Data.Should().NotBeEmpty();
    }
}
