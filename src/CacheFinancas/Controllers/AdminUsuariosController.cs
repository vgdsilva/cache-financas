using System.Security.Claims;
using CacheFinancas.Application.Usuarios;
using CacheFinancas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CacheFinancas.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Usuarios")]
public class AdminUsuariosController(IUsuarioService usuarios) : Controller
{
    private Guid SolicitanteId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var lista = await usuarios.ListarAsync(HttpContext.RequestAborted);
        return View(new UsuariosIndexViewModel { Usuarios = lista, UsuarioLogadoId = SolicitanteId });
    }

    [HttpGet("Criar")]
    public IActionResult Criar() => View(new CriarUsuarioViewModel());

    [HttpPost("Criar")]
    public async Task<IActionResult> Criar(CriarUsuarioViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var r = await usuarios.CriarAsync(model.Nome, model.Email, model.SenhaTemporaria, model.Perfil, HttpContext.RequestAborted);
        if (!r.Sucesso)
        {
            ModelState.AddModelError(string.Empty, r.Erro!);
            return View(model with { SenhaTemporaria = string.Empty });
        }

        TempData["Sucesso"] = "Usuário criado. Ele deverá trocar a senha no primeiro acesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/Ativo")]
    public async Task<IActionResult> AlterarAtivo(Guid id, bool ativo)
    {
        var r = await usuarios.AlterarAtivoAsync(id, ativo, SolicitanteId, HttpContext.RequestAborted);
        TempData[r.Sucesso ? "Sucesso" : "Erro"] = r.Sucesso ? "Usuário atualizado." : r.Erro;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/Desbloquear")]
    public async Task<IActionResult> Desbloquear(Guid id)
    {
        var r = await usuarios.DesbloquearAsync(id, HttpContext.RequestAborted);
        TempData[r.Sucesso ? "Sucesso" : "Erro"] = r.Sucesso ? "Conta desbloqueada." : r.Erro;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/RedefinirSenha")]
    public IActionResult RedefinirSenha(Guid id) => View(new RedefinirSenhaViewModel { Id = id });

    [HttpPost("{id:guid}/RedefinirSenha")]
    public async Task<IActionResult> RedefinirSenha(Guid id, RedefinirSenhaViewModel model)
    {
        if (!ModelState.IsValid) return View(model with { Id = id });

        var r = await usuarios.RedefinirSenhaAsync(id, model.SenhaTemporaria, HttpContext.RequestAborted);
        if (!r.Sucesso)
        {
            ModelState.AddModelError(string.Empty, r.Erro!);
            return View(new RedefinirSenhaViewModel { Id = id });
        }

        TempData["Sucesso"] = "Senha redefinida. O usuário deverá trocá-la no próximo acesso.";
        return RedirectToAction(nameof(Index));
    }
}
