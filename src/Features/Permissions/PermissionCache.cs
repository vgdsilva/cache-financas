namespace VGS.FinanceIA.Features.Permissions;

/// <summary>Chave do cache de menus por usuário, compartilhada entre a query e os commands que a invalidam.</summary>
internal static class PermissionCache
{
    public static string Key(Guid userId) => $"permissions:{userId}";
}
