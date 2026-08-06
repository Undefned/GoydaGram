namespace UserService.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException()
        : base("Resource already exists") { }

    public ConflictException(string message)
        : base(message) { }
}