using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Cqrs;
using VGS.FinanceIA.ViewModels;

namespace VGS.FinanceIA.Controllers;

public class ContaController(ICommandHandler<AuthenticateUserCommand, AuthenticatedUser?> authenticate) : Controller
{
    [AllowAnonymous]
    [HttpGet("/Login")]
    public IActionResult Login(string? returnUrl)
    {
        return User.Identity?.IsAuthenticated == true
            ? LocalRedirect(SafeReturnUrl(returnUrl))
            : View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/Login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await authenticate.HandleAsync(new AuthenticateUserCommand(model.UserName, model.Password), cancellationToken);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");

            return View(model);
        }

        // Só id e nome no cookie: as permissões são consultadas a cada requisição (revogação imediata).
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.UserName)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
    }

    // Só POST (com antiforgery): evita deslogar por um link ou imagem de terceiros.
    [HttpGet("/Logout")]
    public IActionResult Logout() => RedirectToAction("Index", "Home");

    [HttpPost("/Logout")]
    public async Task<IActionResult> LogoutPost()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    private string SafeReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
}
