using System.Text.RegularExpressions;

namespace DDD.Sales.Domain.ValueObjects;

/// <summary>
/// Email Value Object
/// Encapsulates email validation logic
/// </summary>
public sealed class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        email = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));

        return new Email(email);
    }

    public string GetDomain() => Value.Split('@')[1];

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Email other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static implicit operator string(Email email) => email.Value;

    public override string ToString() => Value;
}
