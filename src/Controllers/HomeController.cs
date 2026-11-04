using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.ViewModels;

namespace VGS.FinanceIA.Controllers;

public class HomeController : Controller
{
    /// <summary>A home leva ao Dashboard. A URL vem do catálogo de menus (não depende do nome da action).</summary>
    [HttpGet("/")]
    public IActionResult Index() => LocalRedirect(Menus.UrlOf(Menus.Dashboard)!);

    [AllowAnonymous]
    [HttpGet("/AccessDenied")]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("/Error")]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
