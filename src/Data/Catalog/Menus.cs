namespace VGS.FinanceIA.Data.Catalog;

public sealed record MenuDefinition(string Code, string Description, string? ParentCode, string? Icon, string? Url, int SortOrder);

/// <summary>
/// Catálogo de menus do sistema. Cada página e cada tool de IA declara o código que exige.
/// Para um menu novo: adicione a constante e a definição aqui (pais antes dos filhos);
/// o seeder cria a linha no banco na próxima inicialização. Menu que sair do catálogo é desativado pelo seeder.
/// <para>
/// Estrutura da sidebar: menus de primeiro nível formam a seção "Principal"; um menu sem URL é um agrupador
/// (seção própria, com os filhos abaixo): Cadastros e Configurações.
/// </para>
/// <para>
/// A URL da página segue o caminho do menu (em pt-BR, minúsculo): filho de "Configurações" vive em
/// <c>/configuracoes/usuarios</c>. As ações da tela ficam abaixo dela, em inglês:
/// <c>/create</c>, <c>/edit/{id}</c>, <c>/delete/{id}</c>, <c>/&lt;ação&gt;</c>. Menu sem URL é só agrupador.
/// </para>
/// </summary>
public static class Menus
{
    /// <summary>Título da seção da sidebar com os menus de primeiro nível (os que não estão dentro de um agrupador).</summary>
    public const string MainSectionTitle = "Principal";

    // Principal
    public const string Dashboard = "DASHBOARD";
    public const string Wallet = "WALLET";
    public const string Chat = "CHAT"; // Cacau IA

    // Cadastros
    public const string Registry = "REG";
    public const string Accounts = "REG.ACCOUNTS";
    public const string Transactions = "REG.TRANSACTIONS";
    public const string Categories = "REG.CATEGORIES";

    // Configurações
    public const string Settings = "CONFIG";
    public const string Users = "CONFIG.USERS";

    public static IReadOnlyList<MenuDefinition> All { get; } =
    [
        new(Dashboard, "Painel Financeiro", null, "chart", "/dashboard", 10),
        new(Wallet, "Visão Geral da Carteira", null, "wallet", "/minha-carteira", 20),
        new(Chat, "Cacau IA", null, "chat", "/cacau-ia", 30),

        new(Registry, "Cadastros", null, "folder", null, 50),
        new(Accounts, "Contas", Registry, "bank", "/cadastros/contas", 51),
        new(Transactions, "Lançamentos", Registry, "receipt", "/cadastros/lancamentos", 52),
        new(Categories, "Categorias", Registry, "tag", "/cadastros/categorias", 53),

        new(Settings, "Configurações", null, "gear", null, 90),
        new(Users, "Usuários", Settings, "people", "/configuracoes/usuarios", 91),
    ];

    /// <summary>URL da página de um menu (nula se for só agrupador ou se o código não existir).</summary>
    public static string? UrlOf(string code) => Find(code)?.Url;

    /// <summary>Definição de um menu pelo código (nula se não existir).</summary>
    public static MenuDefinition? Find(string code) => All.FirstOrDefault(m => m.Code == code);
}
