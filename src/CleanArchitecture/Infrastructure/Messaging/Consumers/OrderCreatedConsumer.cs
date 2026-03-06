using CleanArchitecture.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Messaging.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Order created: {OrderUid} for Customer {CustomerUid} - Total: {TotalAmount:C}",
            message.OrderUid, message.CustomerUid, message.TotalAmount);

        return Task.CompletedTask;
    }
}
