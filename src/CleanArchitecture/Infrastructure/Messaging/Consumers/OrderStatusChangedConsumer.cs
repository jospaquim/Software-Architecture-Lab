using CleanArchitecture.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Messaging.Consumers;

public class OrderStatusChangedConsumer : IConsumer<OrderStatusChangedEvent>
{
    private readonly ILogger<OrderStatusChangedConsumer> _logger;

    public OrderStatusChangedConsumer(ILogger<OrderStatusChangedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Order {OrderUid} status changed: {PreviousStatus} -> {NewStatus}",
            message.OrderUid, message.PreviousStatus, message.NewStatus);

        return Task.CompletedTask;
    }
}
