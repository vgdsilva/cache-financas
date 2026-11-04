using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Framework.Authorization;

namespace VGS.FinanceIA.Controllers;

[RequireMenu(Menus.Transactions)]
[Route("cadastros/lancamentos")]
public class LancamentosController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
