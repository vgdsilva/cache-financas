using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VGS.FinanceIA.Data.Catalog;

namespace VGS.FinanceIA.Pages;

/// <summary>A home leva ao Dashboard. A URL vem do catálogo de menus (não depende do nome da página).</summary>
public class IndexModel : PageModel
{
    public IActionResult OnGet() => LocalRedirect(Menus.UrlOf(Menus.Dashboard)!);
}
