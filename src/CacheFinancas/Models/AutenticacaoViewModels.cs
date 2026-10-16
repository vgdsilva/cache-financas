using System.ComponentModel.DataAnnotations;
using CacheFinancas.Application.Usuarios;
using CacheFinancas.Domain.Entities;
using CacheFinancas.Domain.Enums;

namespace CacheFinancas.Models;

public record LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }
}

public record TrocarSenhaViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string SenhaAtual { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [MinLength(Usuario.TamanhoMinimoSenha, ErrorMessage = "A nova senha deve ter ao menos 10 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; init; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NovaSenha), ErrorMessage = "A confirmação não confere.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmarNovaSenha { get; init; } = string.Empty;
}

public record UsuariosIndexViewModel
{
    public IReadOnlyList<UsuarioResumo> Usuarios { get; init; } = [];
    public Guid UsuarioLogadoId { get; init; }
}

public record CriarUsuarioViewModel
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(200)]
    public string Nome { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha temporária.")]
    [MinLength(Usuario.TamanhoMinimoSenha, ErrorMessage = "A senha deve ter ao menos 10 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha temporária")]
    public string SenhaTemporaria { get; init; } = string.Empty;

    public PerfilUsuario Perfil { get; init; } = PerfilUsuario.Usuario;
}

public record RedefinirSenhaViewModel
{
    public Guid Id { get; init; }

    [Required(ErrorMessage = "Informe a senha temporária.")]
    [MinLength(Usuario.TamanhoMinimoSenha, ErrorMessage = "A senha deve ter ao menos 10 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha temporária")]
    public string SenhaTemporaria { get; init; } = string.Empty;
}
