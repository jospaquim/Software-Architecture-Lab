namespace CleanArchitecture.Application.Events;

public record OrderCreatedEvent
{
    public Guid OrderUid { get; init; }
    public Guid CustomerUid { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}
