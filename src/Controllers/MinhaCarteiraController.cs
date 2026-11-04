using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Framework.Authorization;

namespace VGS.FinanceIA.Controllers;

[RequireMenu(Menus.Wallet)]
[Route("minha-carteira")]
public class MinhaCarteiraController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
