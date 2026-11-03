using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Pages;

[AllowAnonymous]
public class LoginModel(ICommandHandler<AuthenticateUserCommand, AuthenticatedUser?> authenticate) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Informe o usuário.")]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe a senha.")]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true ? LocalRedirect(SafeReturnUrl()) : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await authenticate.HandleAsync(new AuthenticateUserCommand(UserName, Password), cancellationToken);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
            return Page();
        }

        // Só id e nome no cookie: as permissões são consultadas a cada requisição (revogação imediata).
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.UserName)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect(SafeReturnUrl());
    }

    private string SafeReturnUrl() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/";
}
