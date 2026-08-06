namespace UserService.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, Guid id) 
        : base($"{entity} with ID {id} not found") { }
    
    public NotFoundException(string entity, string identifier) 
        : base($"{entity} with identifier {identifier} not found") { }
}