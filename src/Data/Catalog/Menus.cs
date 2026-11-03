namespace VGS.FinanceIA.Data.Catalog;

public sealed record MenuDefinition(string Code, string Description, string? ParentCode, string? Icon, string? Url, int SortOrder);

/// <summary>
/// Catálogo de menus do sistema. Cada página e cada tool de IA declara o código que exige.
/// Para um menu novo: adicione a constante e a definição aqui (pais antes dos filhos);
/// o seeder cria a linha no banco na próxima inicialização.
/// </summary>
public static class Menus
{
    public const string Chat = "CHAT";
    public const string Transactions = "FIN.TRANSACTIONS";
    public const string Accounts = "FIN.ACCOUNTS";
    public const string Categories = "FIN.CATEGORIES";
    public const string Settings = "CONFIG";
    public const string Users = "CONFIG.USERS";

    public static IReadOnlyList<MenuDefinition> All { get; } =
    [
        new(Transactions, "Lançamentos", null, "receipt", "/Transactions", 10),
        new(Accounts, "Contas", null, "wallet", "/Accounts", 20),
        new(Categories, "Categorias", null, "tag", "/Categories", 30),
        new(Chat, "Chat", null, "chat", "/Chat", 40),
        new(Settings, "Configurações", null, "gear", null, 90),
        new(Users, "Usuários", Settings, "people", "/Users", 91),
    ];
}
