using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Framework.Authorization;

namespace VGS.FinanceIA.Controllers;

[RequireMenu(Menus.Chat)]
[Route("cacau-ia")]
public class CacauIAController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
