namespace DDD.Sales.Domain.ValueObjects;

/// <summary>
/// Address Value Object
/// Immutable representation of a physical address
/// </summary>
public sealed class Address : IEquatable<Address>
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string ZipCode { get; }

    private Address(string street, string city, string state, string country, string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
    }

    public static Address Create(string street, string city, string state, string country, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("ZipCode is required", nameof(zipCode));

        return new Address(street.Trim(), city.Trim(), state.Trim(), country.Trim(), zipCode.Trim());
    }

    public string GetFullAddress() => $"{Street}, {City}, {State} {ZipCode}, {Country}";

    public bool Equals(Address? other)
    {
        if (other is null) return false;

        return Street == other.Street &&
               City == other.City &&
               State == other.State &&
               Country == other.Country &&
               ZipCode == other.ZipCode;
    }

    public override bool Equals(object? obj) => obj is Address other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Street, City, State, Country, ZipCode);

    public override string ToString() => GetFullAddress();
}
