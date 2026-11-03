namespace VGS.FinanceIA.Domain;

/// <summary>Configuração chave/valor editável em runtime (ex.: modelo de IA).</summary>
public class AppSetting
{
    public required string Key { get; set; }

    public required string Value { get; set; }
}
