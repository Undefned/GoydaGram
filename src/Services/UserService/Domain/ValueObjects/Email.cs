using UserService.Domain.Exceptions;

namespace UserService.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email cannot be empty");

        if (!email.Contains('@') || !email.Contains('.'))
            throw new ValidationException("Invalid email format");

        return new Email(email.ToLowerInvariant());
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string email) => Create(email);
}