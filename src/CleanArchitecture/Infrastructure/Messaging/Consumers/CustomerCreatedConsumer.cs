using CleanArchitecture.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Messaging.Consumers;

public class CustomerCreatedConsumer : IConsumer<CustomerCreatedEvent>
{
    private readonly ILogger<CustomerCreatedConsumer> _logger;

    public CustomerCreatedConsumer(ILogger<CustomerCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CustomerCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Customer created: {CustomerUid} - {FirstName} {LastName} ({Email})",
            message.CustomerUid, message.FirstName, message.LastName, message.Email);

        return Task.CompletedTask;
    }
}
