using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CacheFinancas.Filters;

public static class ClaimsExtras
{
    public const string DeveTrocarSenha = "cf:deve-trocar-senha";
}

/// <summary>Enquanto o usuário precisar trocar a senha, só o controller Conta fica acessível.</summary>
public class ExigirTrocaSenhaFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) return;
        if (user.FindFirst(ClaimsExtras.DeveTrocarSenha)?.Value != "true") return;

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (string.Equals(controller, "Conta", StringComparison.OrdinalIgnoreCase)) return;

        context.Result = new RedirectToActionResult("TrocarSenha", "Conta", null);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
