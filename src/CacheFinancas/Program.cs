using System.Security.Claims;
using System.Threading.RateLimiting;
using CacheFinancas.Application.Abstractions;
using CacheFinancas.Application.Autenticacao;
using CacheFinancas.Domain.Enums;
using CacheFinancas.Filters;
using CacheFinancas.Infrastructure;
using CacheFinancas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CacheFinancas
{
    public class Program
    {
        public const string PoliticaLogin = "login";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews(o =>
            {
                o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                o.Filters.Add<ExigirTrocaSenhaFilter>();
            });
            builder.Services.AddHealthChecks();
            builder.Services.AddCacheFinancas(builder.Configuration);

            // Persiste as chaves do Data Protection (cookies/antiforgery) entre deploys.
            var keysPath = builder.Configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(keysPath))
            {
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
            }

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.Cookie.Name = "__Host-cf";
                    o.Cookie.HttpOnly = true;
                    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    o.Cookie.SameSite = SameSiteMode.Lax;
                    o.Cookie.Path = "/";
                    o.ExpireTimeSpan = TimeSpan.FromHours(8);
                    o.SlidingExpiration = true;
                    o.LoginPath = "/Conta/Login";
                    o.LogoutPath = "/Conta/Logout";
                    o.AccessDeniedPath = "/Conta/AcessoNegado";
                    o.Events.OnValidatePrincipal = async ctx =>
                    {
                        // Conta desativada/excluída ou perfil alterado derruba a sessão existente.
                        var idClaim = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var perfilClaim = ctx.Principal?.FindFirstValue(ClaimTypes.Role);
                        var repo = ctx.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                        var usuario = Guid.TryParse(idClaim, out var id)
                            ? await repo.ObterPorIdAsync(id, ctx.HttpContext.RequestAborted)
                            : null;

                        if (usuario is null || !usuario.PodeEntrar || usuario.Perfil.ToString() != perfilClaim)
                        {
                            ctx.RejectPrincipal();
                            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    };
                });

            builder.Services.AddAuthorization(o =>
            {
                o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });

            builder.Services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.AddPolicy(PoliticaLogin, ctx => RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            });

            var app = builder.Build();

            // Migrations e admin inicial (somente quando habilitado explicitamente no deploy).
            if (app.Configuration.GetValue<bool>("Database:MigrarNoStartup"))
            {
                using var scope = app.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
                await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().ExecutarAsync();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Em producao o TLS e terminado pelo `tailscale serve`; o container fala so HTTP.
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.Use(async (ctx, next) =>
            {
                var h = ctx.Response.Headers;
                h["X-Content-Type-Options"] = "nosniff";
                h["X-Frame-Options"] = "DENY";
                h["Referrer-Policy"] = "strict-origin-when-cross-origin";
                await next();
            });

            app.UseRouting();

            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health").AllowAnonymous();
            app.MapStaticAssets().AllowAnonymous();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
