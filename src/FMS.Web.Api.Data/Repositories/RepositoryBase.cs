using FMS.Domain.entities;
using FMS.Web.Api.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FMS.Web.Api.Data.Repositories;

public abstract class RepositoryBase<T>(AppDbContext context) : IRepository<T> where T : EntityControl
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<T> DbSet { get; } = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        await DbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.SystemIsDeleted = true;
        entity.SystemDateDeleted = DateTime.UtcNow;

        DbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
