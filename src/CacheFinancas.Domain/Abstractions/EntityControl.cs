namespace CacheFinancas.Domain.Abstractions;

public abstract class EntityControl
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? SystemDeleted { get; set; } = null;
    public DateTime? SystemDeletedDate { get; set; } = null;
}
