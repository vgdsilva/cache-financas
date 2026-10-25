using FMS.Web;
using FMS.Web.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = new Uri(
    new Uri(builder.HostEnvironment.BaseAddress),
    builder.Configuration["ApiBaseUrl"] ?? "/api/");

builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, MenuPolicyProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<FmsAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<FmsAuthenticationStateProvider>());
builder.Services.AddScoped<AuthorizationMessageHandler>();

// HttpClient único da aplicação, já com o token. Montado à mão para compartilhar a mesma
// instância do estado de login (handlers do IHttpClientFactory rodam em outro escopo).
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = apiBase };
});

builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
