namespace CleanArchitecture.Application.Events;

public record OrderStatusChangedEvent
{
    public Guid OrderUid { get; init; }
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
}
