namespace FMS.Web.Api.Bussiness.Rules.Features.Auth.Services;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer   { get; set; } = "FMS";
    public string Audience { get; set; } = "FMS";

    /// <summary>Chave de assinatura (HS256), mínimo de 32 caracteres. Em produção vem de variável de ambiente.</summary>
    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays   { get; set; } = 7;
}
