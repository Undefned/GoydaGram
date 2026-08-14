using ContentService.Domain.Entities;

namespace ContentService.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    IUnitOfWork UnitOfWork { get; }
}