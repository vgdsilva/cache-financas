# CacheFinancas

App pessoal (uso individual) em .NET 10 / ASP.NET Core Razor Pages + HTMX, rodando em homeserver (Docker) e usado no iPhone como PWA.
Dois modos com as MESMAS regras de negócio: telas de CRUD (operacional) e chat em linguagem natural (IA via tool use).

Idioma: comentários, README e respostas em pt-BR. Código, nomes de classes e commits em inglês, **exceto as páginas** (`Pages/`): pastas e arquivos `.cshtml` em pt-BR, sem acento (ver "Rotas das páginas").

## Regra de ouro

**PageModel e IA (tools) só chamam Handlers de Command/Query. Regra de negócio só nos Handlers (em `Features/`).**
Nenhuma regra em PageModel, endpoint, tool, DbContext ou código de `Framework/`.

## Arquitetura: Vertical Slice + CQRS

Cada caso de uso é um arquivo em `Features/<Feature>/` com o Command ou Query (um `record`) e o seu handler:

```csharp
public sealed record GrantMenuAccessCommand(Guid UserId, string MenuCode) : ICommand;
public sealed class GrantMenuAccessHandler(...) : ICommandHandler<GrantMenuAccessCommand> { ... }
```

- `ICommand` / `ICommand<TResult>`: mudam estado. `IQuery<TResult>`: só leem, nunca alteram estado.
- Interfaces em `Framework/Cqrs`. **Sem biblioteca de mediator**: a página ou a tool injeta o handler que precisa (`ICommandHandler<...>` / `IQueryHandler<...>`).
- Handlers são registrados automaticamente por varredura de assembly (`AddHandlers`); handler novo não precisa de registro manual.
- Um handler pode chamar outro por injeção (ex.: `HasMenuAccessHandler` usa `GetAllowedMenusQuery`).
- Resultados saem como `record` próprio da feature (ex.: `AuthenticatedUser`, `MenuEntry`), nunca a entidade do EF para a UI.

## Estrutura

```
src/                        # UM único projeto web (CacheFinancas.csproj fica direto aqui)
  Program.cs                # enxuto: só chama os métodos de extensão do Framework
  Framework/                # tudo que é configuração/infra do ASP.NET (nenhuma regra de negócio)
    Authentication/         #   cookie auth (inclui redirect amigável para HTMX)
    Authorization/          #   RequireMenuAttribute, MenuPolicyProvider, MenuAuthorizationHandler, CurrentUser
    Cqrs/                   #   interfaces de Command/Query/Handler e registro automático
    Extensions/             #   AddFrameworkDatabase, UseFramework, MapFramework
  Features/                 # regra de negócio: <Feature>/<UseCase>.cs (command/query + handler)
    Permissions/            #   GetAllowedMenus, HasMenuAccess, GrantMenuAccess, RevokeMenuAccess
    Users/                  #   CreateUser, AuthenticateUser, EnsureInitialAdmin
  Data/
    AppDbContext.cs         #   só DbSet<T> + regras transversais de EntityControl
    Types/                  #   ENTIDADES do EF (EntityControl, User, Configuration, MenuItem, ...)
    Catalog/                #   CATÁLOGOS dessas entidades, definidos no código (Menus, ConfigurationKeys)
    DatabaseSeeder.cs       #   sincroniza os catálogos e cria o admin inicial
    Migrations/
  Pages/                    # Razor Pages: camada fina de UI, só chamam handlers
  ViewComponents/           # AppNav (topbar mobile + sidebar desktop, só menus liberados), NavIcons
  wwwroot/                  # css, js, manifest, service worker
tests/VGS.FinanceIA.Tests/  # espelha src/ (xUnit + Testcontainers para Postgres)
deploy/                     # Dockerfile, docker-compose.yml, .env.example
```

Previsto: `Ai/` (ChatService, tools). Domínio: controle de finanças (contas, categorias, lançamentos).
Feature nova = pasta em `Features/` + (se precisar de tela) página em `Pages/` + (se precisar de menu) entrada em `Data/Catalog/Menus.cs`.

## Convenções

- `net10.0`, C# 14, `Nullable` enabled, `TreatWarningsAsErrors` (config em `Directory.Build.props`).
- Namespaces file-scoped; campos privados `_camelCase` (ver `.editorconfig`).
- HTMX para interatividade; sem SPA, sem Blazor, sem Node no build.
- Minimal APIs apenas para chat (SSE) e `/health`.
- Modelo de IA configurável via tabela de configurações no banco (`Ai:Model`, padrão em `Data/Catalog/ConfigurationKeys.cs`); `ANTHROPIC_API_KEY` vem do ambiente.
- Commits pequenos e semânticos (`feat:`, `fix:`, `chore:`, `test:`, `docs:`), em inglês. Trabalho direto na `develop`.

## Comandos

```
dotnet build
dotnet tool restore  # uma vez (instala o dotnet-ef local)
dotnet tool run dotnet-ef migrations add <Nome> --project src --output-dir Data/Migrations   # após mudar entidade
dotnet tool run dotnet-ef database update --project src                                      # aplica no banco da ConnectionStrings:Default
dotnet test          # testes de integração exigem Docker (Testcontainers); sem Docker aparecem como "Ignorado"
```

## Rotas das páginas

**A URL da página é o caminho do menu**, em pt-BR e minúsculo. A hierarquia do catálogo de menus vira a hierarquia da URL: Usuários é filho de Configurações, então vive em `/configuracoes/usuarios`. Menus de primeiro nível: `/dashboard`, `/minha-carteira`, `/cacau-ia`. Grupo Cadastros: `/cadastros/contas`, `/cadastros/lancamentos`, `/cadastros/categorias`. Grupo Configurações: `/configuracoes/usuarios`.

- Ações da tela ficam **abaixo** da URL do menu, com nome em inglês: `/<menu>/create`, `/<menu>/edit/{id:guid}`, `/<menu>/delete/{id:guid}` e `/<menu>/<ação>` para as demais. A listagem é a própria URL do menu.
- **Páginas em pt-BR**: pasta e arquivo seguem o menu, sem acento (`Pages/Configuracoes/Usuarios/Index.cshtml`, `Pages/Cadastros/Contas/Index.cshtml`, `Pages/MinhaCarteira/`, `Pages/CacauIA/`). Páginas de ação também (`Criar.cshtml`, `Editar.cshtml`, `Excluir.cshtml`), mas a **rota** da ação continua em inglês (`/create`, `/edit/{id}`, `/delete/{id}`). Todo o resto do C# (handlers, entidades, classes) segue em inglês. Rota explícita na diretiva: `@page "/configuracoes/usuarios"`, `@page "/configuracoes/usuarios/edit/{id:guid}"`. Não dependa do nome da pasta para formar a URL.
- A URL do menu fica em `Data/Catalog/Menus.cs` e é a fonte da verdade; o seeder regrava a URL dos menus existentes na subida. Menu sem URL é só agrupador (título de seção na sidebar).
- Toda página sob a URL de um menu declara `[RequireMenu(Menus.X)]` desse mesmo menu, e toda página que exige um menu vive sob a URL dele. Os testes `Menus_*` e `PageRoutesTests` garantem isso.
- Fora dessa regra (não são menu): `/Login`, `/Logout`, `/AccessDenied`, `/Error`, `/health`.

## Cadastro de usuários (`/configuracoes/usuarios`)

- Telas em `Pages/Configuracoes/Usuarios/`: `Index` (lista + busca `?q=`), `Criar` (`/create`), `Editar` (`/edit/{id}`), `Senha` (`/password/{id}`, redefinir senha) e `Excluir` (`/delete/{id}`, confirmação). `Criar` e `Editar` dividem o `_Formulario` (base `UsuarioFormPageModel`).
- Handlers em `Features/Users` (`ListUsers`, `GetUser`, `RegisterUser`, `UpdateUser`, `ChangeUserPassword`, `DeleteUser`) e `Features/Permissions` (`SetUserMenus`, `ListAssignableMenus`). `CreateUser` é o bloco básico, **sem** checar quem pede (usado pelo seed); a tela usa `RegisterUser`.
- **Só administrador gerencia usuários** (criar, editar, redefinir senha, excluir): imposto nos handlers por `UserRules.RequireAdminAsync`. As páginas só escondem botões/redirecionam para `/AccessDenied`. Motivo: usuário comum com o menu "Usuários" poderia criar admins ou conceder menus que não tem.
- Proteções: ninguém exclui a si mesmo; sempre sobra pelo menos um administrador (ao excluir ou ao tirar o perfil de admin). Excluir é lógico e leva as permissões e configurações do usuário. Nome liberado pode ser reutilizado.
- Administrador acessa todos os menus, então a escolha de menus só vale para usuário comum (fica desabilitada no formulário e é ignorada pelos handlers).
- Só os menus que são páginas (com URL) são atribuíveis; `SetUserMenus` concede/revoga para ficar exatamente igual ao conjunto enviado e invalida o cache de permissões.
- Erro de regra de negócio = `BusinessRuleException` (`Framework/Cqrs`), com mensagem para o usuário final. A UI captura **só** essa (via `TryRunAsync`) e mostra no formulário; qualquer outra exceção é bug e segue como erro. Mensagem de sucesso vai em `TempData["Success"]` (o `_Layout` exibe).
- **Campos do usuário** (`users`): `UserName` (login), `Email`, `FullName`, `PasswordHash`, `IsAdmin`.
  - **Usuário (login)**: só letras, dígitos, ponto, hífen e underscore, 3 a 40 caracteres; gravado em minúsculas; único entre os ativos.
  - **E-mail**: obrigatório e com formato válido; gravado em minúsculas. Não é único (o login é o usuário). Usuários anteriores ao campo ficam com e-mail vazio até a próxima edição, que o exige.
  - **Nome completo**: opcional (até 120); quando existe aparece no cabeçalho e nas listas no lugar do login (`DisplayName`).
  - As regras de formato ficam em `Features/Users/UserRules`. Os handlers apontam o campo no `BusinessRuleException(message, field)` e a página mostra a mensagem junto do campo (`FieldKey`).
- **Formulário**: campos em grade (usuário, e-mail, nome completo, senha ao criar), chave **Administrador** ("Acesso total a todos os menus do sistema") e bloco **Menus de Acesso** em cartões por grupo, com contagem `(marcados/total)` e botões Marcar tudo/Limpar/Marcar por grupo (JS, aprimoramento progressivo). A chave desabilita o bloco de menus.

## Layout e navegação

- CSS e JS próprios (`wwwroot/css/site.css`, `wwwroot/js/site.js`), sem Bootstrap/Tailwind e sem Node. Cores e medidas são variáveis em `:root`.
- **O shell logado reproduz o layout de referência** (app Next/Tailwind do trabalho): sidebar de 18rem com cabeçalho (marca + recolher/fechar), seções com título em caixa-alta de 10px, item ativo preenchido com a cor da marca; header de 3.5rem com hambúrguer (só mobile), atalho do chat e menu do usuário (avatar, nome, perfil, dropdown com Sair); conteúdo rola dentro de `<main>`. Medidas e cinzas são as do Tailwind; o breakpoint é o `md` (768px).
- Mobile (< 768px): a sidebar é uma **gaveta** que desliza da esquerda (hambúrguer no header, X no cabeçalho da sidebar, fundo escurecido, Esc fecha). Desktop (≥ 768px): sidebar fixa, recolhível só com ícones (estado em `localStorage`, aplicado no `<head>` para não piscar).
- Sidebar = `AppNav` (menus liberados); header = `AppHeader` (perfil via `GetUserProfileQuery`, só exibição). Toda seção tem título: menus de primeiro nível ficam em "Principal".
- Não temos as abas de domínio nem o sino de notificações da referência (não há o que mostrar); entram quando houver conteúdo real.
- `_Layout` mostra o shell só para usuário autenticado; `_AuthLayout` é o da tela de login (formulário + painel de marca).
- A sidebar é montada a partir de `GetAllowedMenusQuery`: menu **sem URL** é agrupador (título de seção) e os filhos liberados ficam abaixo; os demais vão na seção "Principal". O ícone vem do campo `Icon` do catálogo e é resolvido em `NavIcons` (nome desconhecido usa um ícone padrão; ícone novo = entrada em `NavIcons`).
- **Tema claro/escuro/sistema**: as cores são tokens com `light-dark(claro, escuro)` em `:root` (uma paleta só, sem bloco duplicado). Padrão = segue o aparelho (`color-scheme: light dark`); a escolha do usuário (menu do usuário → Tema) vira `data-theme="light|dark"` no `<html>` e fica em `localStorage` (`theme`; "Sistema" remove a chave). O script inline de `_Head.cshtml` aplica isso antes da primeira pintura (precisa vir antes do CSS). Cor nova = token em `:root` com os dois valores; não use cor fixa nos componentes. Requer Safari 17.5+/Chrome 123+ (`light-dark()`).
- **Páginas usam a largura inteira da tela**: sem `max-width` em página, formulário, tabela ou cartão (o conteúdo preenche o `<main>`). Só são aceitos limites que não são de largura de página: a gaveta do menu no mobile (`86vw`), o truncamento do nome no header e o cartão de login (tela centralizada própria).
- Não use `section` como nome de variável em `.cshtml` (palavra reservada do Razor).

## Entidades e banco

- Entidades ficam em `Data/Types/`; **catálogos** (listas de itens conhecidos, definidas no código) em `Data/Catalog/`.
- Toda entidade herda de `EntityControl` e é configurada **por atributos** (`[Table]`, `[Column]`, `[Index]`, `[ForeignKey]`); o `AppDbContext` só expõe `DbSet<T>` e aplica regras transversais.
- Datas de auditoria em UTC (`timestamptz`); datas de negócio (vencimento, competência) em `DateOnly`/`date`.
- Exclusão lógica: `system_deleted = Guid.Empty` (ativo) ou o `Id` do próprio registro (excluído). O filtro global e a conversão de `Remove` estão no `AppDbContext`.
- Índice único sempre inclui `SystemDeleted` (ex.: `[Index(nameof(UserName), nameof(SystemDeleted), IsUnique = true)]`). Nulos contam como iguais.
- Catálogo novo: o `DatabaseSeeder` cria o que falta, nunca sobrescreve valor editado e não recria item excluído de propósito.

## Autorização por menu

- Acesso é por **menu** (sim/não), atribuído **direto ao usuário** (`UserMenuPermission`). `User.IsAdmin` acessa todos os menus ativos.
- O catálogo de menus fica em `Data/Catalog/Menus.cs` (códigos estáveis, ex.: `REG.TRANSACTIONS`, filho do grupo `REG`). Menu novo = constante + definição lá (pais antes dos filhos); o `DatabaseSeeder` cria a linha na próxima inicialização, acompanha texto/ícone/URL/ordem/pai dos existentes e **desativa** (sem apagar) o que saiu do catálogo. Estrutura atual: Principal (Dashboard, Minha Carteira, Cacau IA = chat), Cadastros (Contas, Lançamentos, Categorias), Configurações (Usuários).
- A pergunta "quem acessa o quê" é respondida só por `HasMenuAccessQuery`. As permissões **não** vão no cookie (só id e nome): revogar vale na hora.
- Toda página declara o menu que exige: `[RequireMenu(Menus.Transactions)]` no PageModel (cobre os handlers HTMX da página). Sem atributo, a página exige apenas usuário autenticado; `[AllowAnonymous]` só para Login, AccessDenied e Error.
- Toda tool de IA declara o menu que exige (`RequiredMenu`); o `ToolRegistry` só oferece ao modelo as tools permitidas e revalida (com `HasMenuAccessQuery`) antes de executar.
- Admin inicial: `Seed:AdminUserName`, `Seed:AdminPassword` e `Seed:AdminEmail` (variáveis `Seed__AdminUserName`, `Seed__AdminPassword` e `Seed__AdminEmail`), criado só se não houver usuários. Senha com 8+ caracteres por padrão; `Auth:MinPasswordLength` reduz o mínimo e **só deve ser usado em desenvolvimento** (o `appsettings.Development.json` usa `admin`/`123`, e-mail `admin@cachefinancas.local`, com mínimo 3; nunca coloque isso em Production).
- Na subida, o `DatabaseInitializer` aplica as **migrations pendentes** e depois roda o seeder. Desligue ambos com `Database:InitializeOnStartup=false` (aí o schema é aplicado por fora, com `dotnet ef database update`). Migration nova continua sendo gerada à mão (`migrations add`); o teste `Model_IsInSyncWithMigrations` avisa se esquecer.
