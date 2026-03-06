using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Customer entity - Represents a customer in the e-commerce system
/// Follows SOLID principles:
/// - SRP: Only responsible for customer data and business rules
/// - OCP: Can be extended without modification
/// </summary>
public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsVip { get; set; } = false;
    public DateTime? BirthDate { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    // Business rules
    public string GetFullName() => $"{FirstName} {LastName}";

    public int GetAge()
    {
        if (!BirthDate.HasValue)
            return 0;

        var today = DateTime.Today;
        var age = today.Year - BirthDate.Value.Year;

        if (BirthDate.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }

    public void Deactivate()
    {
        IsActive = false;
        AddDomainEvent(new CustomerDeactivatedEvent(Id, Email));
    }

    public void Activate()
    {
        IsActive = true;
        AddDomainEvent(new CustomerActivatedEvent(Id, Email));
    }

    public void PromoteToVip()
    {
        if (!IsVip)
        {
            IsVip = true;
            AddDomainEvent(new CustomerPromotedToVipEvent(Id, Email));
        }
    }
}

// Domain Events
public class CustomerDeactivatedEvent : DomainEvent
{
    public int CustomerId { get; }
    public string Email { get; }

    public CustomerDeactivatedEvent(int customerId, string email)
    {
        CustomerId = customerId;
        Email = email;
    }
}

public class CustomerActivatedEvent : DomainEvent
{
    public int CustomerId { get; }
    public string Email { get; }

    public CustomerActivatedEvent(int customerId, string email)
    {
        CustomerId = customerId;
        Email = email;
    }
}

public class CustomerPromotedToVipEvent : DomainEvent
{
    public int CustomerId { get; }
    public string Email { get; }

    public CustomerPromotedToVipEvent(int customerId, string email)
    {
        CustomerId = customerId;
        Email = email;
    }
}
