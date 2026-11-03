var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Exposes the entry point to WebApplicationFactory in the test project.
public partial class Program;
