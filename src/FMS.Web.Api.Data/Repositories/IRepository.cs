using FMS.Domain.entities;

namespace FMS.Web.Api.Data.Repositories;

public interface IRepository<T> where T : EntityControl
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Exclusão lógica: marca SystemIsDeleted e SystemDateDeleted.</summary>
    Task RemoveAsync(T entity, CancellationToken cancellationToken = default);
}
