using CacheFinancas.Server.Config;
using CacheFinancas.Server.Database;
using CacheFinancas.Server.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddWebCors(builder.Configuration);
builder.Services.AddModules(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseWebCors();
app.MapModules();

await app.MigrateDatabaseAsync();
app.Run();
