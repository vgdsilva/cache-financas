using System.ComponentModel.DataAnnotations;

namespace VGS.FinanceIA.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Informe o usuário.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
