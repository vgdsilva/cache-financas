using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Framework.Authorization;

namespace VGS.FinanceIA.Controllers;

[RequireMenu(Menus.Accounts)]
[Route("cadastros/contas")]
public class ContasController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
