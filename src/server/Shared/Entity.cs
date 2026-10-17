namespace CacheFinancas.Server.Shared;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CriadoEm { get; protected set; } = DateTime.UtcNow;
}
