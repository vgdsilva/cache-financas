using FMS.Domain.entities;
using FMS.Web.Api.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FMS.Web.Api.Data.Repositories;

public interface IUserMenuPermissionRepository : IRepository<UserMenuPermission>
{
    /// <summary>Permissões do usuário em menus ativos, já com o MenuItem carregado.</summary>
    Task<IReadOnlyList<UserMenuPermission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class UserMenuPermissionRepository(AppDbContext context)
    : RepositoryBase<UserMenuPermission>(context), IUserMenuPermissionRepository
{
    public async Task<IReadOnlyList<UserMenuPermission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(x => x.MenuItemEntity)
            .Where(x => x.UserId == userId && x.MenuItemEntity != null && x.MenuItemEntity.IsActive)
            .ToListAsync(cancellationToken);
}
