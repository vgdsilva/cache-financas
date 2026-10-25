namespace FMS.Web.Api.Data.Seed;

public class SeedOptions
{
    public const string Section = "Seed:Admin";

    public string Name     { get; set; } = "Administrador";
    public string Login    { get; set; } = "admin";
    public string Email    { get; set; } = "admin@fms.local";

    /// <summary>Senha do admin inicial. Vazia = o usuário não é criado.</summary>
    public string Password { get; set; } = string.Empty;
}
