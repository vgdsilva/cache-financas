using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Framework.Authorization;

namespace VGS.FinanceIA.Controllers;

[RequireMenu(Menus.Dashboard)]
[Route("dashboard")]
public class DashboardController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
