namespace CleanArchitecture.Application.Events;

public record CustomerCreatedEvent
{
    public Guid CustomerUid { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
