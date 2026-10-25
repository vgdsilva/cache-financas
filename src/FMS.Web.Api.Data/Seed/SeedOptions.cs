namespace FMS.Web.Api.Data.Seed;

public class SeedOptions
{
    public const string Section = "Seed";

    public SeedUser Admin { get; set; } = new()
    {
        Name = "Administrador",
        Login = "admin",
        Email = "admin@fms.local"
    };

    /// <summary>Usuário comum (sem role Admin), útil em desenvolvimento. Sem senha = não é criado.</summary>
    public SeedUser User { get; set; } = new()
    {
        Name = "Usuário",
        Login = "usuario",
        Email = "usuario@fms.local"
    };
}

public class SeedUser
{
    public string Name  { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Vazia = o usuário não é criado.</summary>
    public string Password { get; set; } = string.Empty;
}
