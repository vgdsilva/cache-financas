using System.Text.Json;
using System.Text.Json.Serialization;

namespace FMS.Communication.Features.Auth.Dtos;

/// <summary>
/// Menu liberado ao usuário, no formato compacto que viaja dentro do JWT (claim <see cref="MenuClaims.ClaimType"/>).
/// </summary>
public class MenuClaim
{
    public const int View = 1, Create = 2, Edit = 4, Delete = 8;

    [JsonPropertyName("i")]  public Guid    Id          { get; set; }
    [JsonPropertyName("p")]  public Guid?   ParentId    { get; set; }
    [JsonPropertyName("c")]  public string  Code        { get; set; } = string.Empty;
    [JsonPropertyName("d")]  public string  Description { get; set; } = string.Empty;
    [JsonPropertyName("ic")] public string? Icon        { get; set; }
    [JsonPropertyName("u")]  public string? Url         { get; set; }
    [JsonPropertyName("o")]  public int     Ordem       { get; set; }

    /// <summary>Máscara de permissões: 1 = ver, 2 = criar, 4 = editar, 8 = excluir.</summary>
    [JsonPropertyName("a")]  public int     Access      { get; set; }

    [JsonIgnore] public bool CanView   => (Access & View) != 0;
    [JsonIgnore] public bool CanCreate => (Access & Create) != 0;
    [JsonIgnore] public bool CanEdit   => (Access & Edit) != 0;
    [JsonIgnore] public bool CanDelete => (Access & Delete) != 0;
}

public static class MenuClaims
{
    public const string ClaimType = "menus";

    public static string Serialize(IEnumerable<UserPermissionDto> permissions)
        => JsonSerializer.Serialize(permissions.Select(p => new MenuClaim
        {
            Id = p.MenuItemId,
            ParentId = p.MenuItemSuperiorId,
            Code = p.Code,
            Description = p.Description,
            Icon = p.Icon,
            Url = p.URL,
            Ordem = p.Ordem,
            Access = (p.CanView ? MenuClaim.View : 0)
                   | (p.CanCreate ? MenuClaim.Create : 0)
                   | (p.CanEdit ? MenuClaim.Edit : 0)
                   | (p.CanDelete ? MenuClaim.Delete : 0)
        }));

    public static List<MenuClaim> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<MenuClaim>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
