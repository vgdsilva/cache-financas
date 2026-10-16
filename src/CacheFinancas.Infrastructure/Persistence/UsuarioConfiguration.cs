using CacheFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CacheFinancas.Infrastructure.Persistence;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).IsRequired().HasMaxLength(200);
        // E-mail é sempre gravado em minúsculas (Usuario.NormalizarEmail), então o índice único já é case-insensitive.
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.Property(u => u.SenhaHash).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Perfil).HasConversion<int>();

        // Soft delete: usuários excluídos não aparecem nem bloqueiam o reuso do e-mail.
        builder.HasQueryFilter(u => u.SystemDeleted == null);
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("\"SystemDeleted\" IS NULL");
    }
}
