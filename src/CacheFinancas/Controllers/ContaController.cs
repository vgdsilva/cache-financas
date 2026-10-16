using System.Security.Claims;
using CacheFinancas.Application.Autenticacao;
using CacheFinancas.Filters;
using CacheFinancas.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CacheFinancas.Controllers;

public class ContaController(IAutenticacaoService autenticacao) : Controller
{
    private const string MensagemCredenciais = "E-mail ou senha inválidos.";
    private const string MensagemBloqueio = "Acesso temporariamente bloqueado. Tente novamente mais tarde.";

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting(Program.PoliticaLogin)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var resultado = await autenticacao.LoginAsync(model.Email, model.Senha, HttpContext.RequestAborted);

        if (resultado.Status == StatusLogin.Bloqueado)
        {
            ModelState.AddModelError(string.Empty, MensagemBloqueio);
            return View(model with { Senha = string.Empty });
        }

        if (resultado.Status != StatusLogin.Sucesso || resultado.Usuario is null)
        {
            ModelState.AddModelError(string.Empty, MensagemCredenciais);
            return View(model with { Senha = string.Empty });
        }

        await EmitirCookieAsync(resultado.Usuario);

        if (resultado.Usuario.DeveTrocarSenha) return RedirectToAction(nameof(TrocarSenha));
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult TrocarSenha() => View(new TrocarSenhaViewModel());

    [HttpPost]
    public async Task<IActionResult> TrocarSenha(TrocarSenhaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var resultado = await autenticacao.TrocarSenhaAsync(id, model.SenhaAtual, model.NovaSenha, HttpContext.RequestAborted);

        if (!resultado.Sucesso)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro ?? "Não foi possível trocar a senha.");
            return View(new TrocarSenhaViewModel());
        }

        // Reemite o cookie sem a exigência de troca de senha.
        var atual = new UsuarioAutenticado(
            id,
            User.FindFirstValue(ClaimTypes.Name)!,
            User.FindFirstValue(ClaimTypes.Email)!,
            Enum.Parse<Domain.Enums.PerfilUsuario>(User.FindFirstValue(ClaimTypes.Role)!),
            DeveTrocarSenha: false);
        await EmitirCookieAsync(atual);

        TempData["Sucesso"] = "Senha alterada com sucesso.";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AcessoNegado() => StatusCode(StatusCodes.Status403Forbidden, View());

    private Task EmitirCookieAsync(UsuarioAutenticado u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nome),
            new(ClaimTypes.Email, u.Email),
            new(ClaimTypes.Role, u.Perfil.ToString()),
            new(ClaimsExtras.DeveTrocarSenha, u.DeveTrocarSenha ? "true" : "false")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });
    }
}
