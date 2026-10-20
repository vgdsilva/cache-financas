using FMS.Migration.Seeds;
using FMS.Web.Api.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default nao configurada.");

builder.Services.AddAppDbContext(connectionString, typeof(Program).Assembly.GetName().Name);

// O "dotnet ef" interrompe a execucao logo apos o Build(); o que vem depois so roda de verdade.
using var host = builder.Build();
using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migration");
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

logger.LogInformation("Aplicando migrations...");
await context.Database.MigrateAsync();

logger.LogInformation("Executando seeds...");
await DatabaseSeeder.SeedAsync(context);

logger.LogInformation("Banco de dados atualizado.");
