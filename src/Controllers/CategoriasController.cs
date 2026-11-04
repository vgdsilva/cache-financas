using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Framework.Authorization;

namespace VGS.FinanceIA.Controllers;

[RequireMenu(Menus.Categories)]
[Route("cadastros/categorias")]
public class CategoriasController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
