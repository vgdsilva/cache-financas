using VGS.FinanceIA.Framework.Authentication;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;
using VGS.FinanceIA.Framework.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

builder.Services.AddFrameworkDatabase(builder.Configuration);
builder.Services.AddFrameworkAuthentication();
builder.Services.AddFrameworkAuthorization();

// Handlers de Command/Query das Features (PageModel e IA só chamam estes).
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();

app.UseFramework();
app.MapFramework();

app.Run();

// Exposes the entry point to WebApplicationFactory in the test project.
public partial class Program;
