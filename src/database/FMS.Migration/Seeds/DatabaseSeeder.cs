using FMS.Web.Api.Infraestructure.Data;

namespace FMS.Migration.Seeds;

public static class DatabaseSeeder
{
    // Seeds devem ser idempotentes: rodam a cada deploy.
    public static Task SeedAsync(AppDbContext context, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
