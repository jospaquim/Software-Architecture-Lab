using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

public class Address : BaseEntity
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;

    // Foreign key
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string GetFullAddress()
    {
        return $"{Street}, {City}, {State} {ZipCode}, {Country}";
    }
}
