namespace VGS.FinanceIA.Data.Catalog;

public sealed record ConfigurationDefinition(string Key, string DefaultValue);

/// <summary>
/// Catálogo das configurações globais conhecidas (UserId nulo) e seus valores padrão.
/// O seeder cria as que faltam e nunca sobrescreve um valor já editado.
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>Modelo do Claude usado pelo chat. Editável em runtime.</summary>
    public const string AiModel = "Ai:Model";

    public static IReadOnlyList<ConfigurationDefinition> All { get; } =
    [
        new(AiModel, "claude-sonnet-5-5"),
    ];
}
