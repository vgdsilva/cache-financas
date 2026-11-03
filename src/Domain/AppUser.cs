namespace VGS.FinanceIA.Domain;

public class AppUser
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string UserName { get; set; }

    public required string PasswordHash { get; set; }
}
