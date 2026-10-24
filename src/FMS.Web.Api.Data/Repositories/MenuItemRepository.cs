using FMS.Domain.entities;
using FMS.Web.Api.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FMS.Web.Api.Data.Repositories;

public interface IMenuItemRepository : IRepository<MenuItem>
{
    Task<IReadOnlyList<MenuItem>> GetActiveAsync(CancellationToken cancellationToken = default);
}

public class MenuItemRepository(AppDbContext context) : RepositoryBase<MenuItem>(context), IMenuItemRepository
{
    public async Task<IReadOnlyList<MenuItem>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Ordem).ToListAsync(cancellationToken);
}
