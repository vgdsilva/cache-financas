using FMS.Communication.Features.Auth.Dtos;
using FMS.Domain.entities;
using FMS.Web.Api.Data.Repositories;

namespace FMS.Web.Api.Bussiness.Rules.Features.Auth.Services;

public interface ILoggedUserBuilder
{
    Task<LoggedUserDto> BuildAsync(User user, CancellationToken cancellationToken = default);
}

public class LoggedUserBuilder(
    IMenuItemRepository menuItems,
    IUserMenuPermissionRepository permissions) : ILoggedUserBuilder
{
    public async Task<LoggedUserDto> BuildAsync(User user, CancellationToken cancellationToken = default)
    {
        var dto = new LoggedUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Login = user.Login,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            MustChangePassword = user.MustChangePassword,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LastLoginAt = user.LastLoginAt
        };

        if (user.IsAdmin)
        {
            // Administrador enxerga e altera todos os menus ativos.
            dto.Permissions = (await menuItems.GetActiveAsync(cancellationToken))
                .Select(m => ToDto(m, canView: true, canCreate: true, canEdit: true, canDelete: true))
                .ToList();
        }
        else
        {
            dto.Permissions = (await permissions.GetByUserIdAsync(user.Id, cancellationToken))
                .Where(p => p.CanView)
                .OrderBy(p => p.MenuItemEntity!.Ordem)
                .Select(p => ToDto(p.MenuItemEntity!, p.CanView, p.CanCreate, p.CanEdit, p.CanDelete))
                .ToList();
        }

        return dto;
    }

    private static UserPermissionDto ToDto(MenuItem menu, bool canView, bool canCreate, bool canEdit, bool canDelete) => new()
    {
        MenuItemId = menu.Id,
        MenuItemSuperiorId = menu.MenuItemSuperiorId,
        Code = menu.Code,
        Description = menu.Description,
        Icon = menu.Icon,
        URL = menu.URL,
        Page = menu.Page,
        Ordem = menu.Ordem,
        CanView = canView,
        CanCreate = canCreate,
        CanEdit = canEdit,
        CanDelete = canDelete
    };
}
