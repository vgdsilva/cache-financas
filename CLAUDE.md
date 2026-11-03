# VGS.FinanceIA

App pessoal (uso individual) em .NET 10 / ASP.NET Core Razor Pages + HTMX, rodando em homeserver (Docker) e usado no iPhone como PWA.
Dois modos com as MESMAS regras de negócio: telas de CRUD (operacional) e chat em linguagem natural (IA via tool use).

Idioma: comentários, README e respostas em pt-BR. Código, nomes de classes e commits em inglês.

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
src/                        # UM único projeto web (VGS.FinanceIA.csproj fica direto aqui)
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

## Layout e navegação

- CSS e JS próprios (`wwwroot/css/site.css`, `wwwroot/js/site.js`), sem Bootstrap/Tailwind e sem Node. Cores e medidas são variáveis em `:root` (claro/escuro por `prefers-color-scheme`).
- Mobile (< 992px): topbar com hambúrguer; o menu **desce** abaixo dela (estilo navbar do Bootstrap). Desktop: sidebar fixa, recolhível só com ícones (estado em `localStorage`, aplicado no `<head>` para não piscar).
- `_Layout` mostra o shell só para usuário autenticado; `_AuthLayout` é o da tela de login (formulário + painel de marca).
- A sidebar é montada a partir de `GetAllowedMenusQuery`: menu **sem URL** é agrupador (título de seção) e os filhos liberados ficam abaixo; os demais vão na seção do topo. O ícone vem do campo `Icon` do catálogo e é resolvido em `NavIcons` (nome desconhecido usa um ícone padrão; ícone novo = entrada em `NavIcons`).
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
- O catálogo de menus fica em `Data/Catalog/Menus.cs` (códigos estáveis, ex.: `FIN.TRANSACTIONS`). Menu novo = constante + definição lá (pais antes dos filhos); o `DatabaseSeeder` cria a linha na próxima inicialização.
- A pergunta "quem acessa o quê" é respondida só por `HasMenuAccessQuery`. As permissões **não** vão no cookie (só id e nome): revogar vale na hora.
- Toda página declara o menu que exige: `[RequireMenu(Menus.Transactions)]` no PageModel (cobre os handlers HTMX da página). Sem atributo, a página exige apenas usuário autenticado; `[AllowAnonymous]` só para Login, AccessDenied e Error.
- Toda tool de IA declara o menu que exige (`RequiredMenu`); o `ToolRegistry` só oferece ao modelo as tools permitidas e revalida (com `HasMenuAccessQuery`) antes de executar.
- Admin inicial: `Seed:AdminUserName` e `Seed:AdminPassword` (variáveis `Seed__AdminUserName` e `Seed__AdminPassword`), criado só se não houver usuários. Senha com 8+ caracteres por padrão; `Auth:MinPasswordLength` reduz o mínimo e **só deve ser usado em desenvolvimento** (o `appsettings.Development.json` usa `admin`/`123` com mínimo 3; nunca coloque isso em Production).
- Na subida, o `DatabaseInitializer` aplica as **migrations pendentes** e depois roda o seeder. Desligue ambos com `Database:InitializeOnStartup=false` (aí o schema é aplicado por fora, com `dotnet ef database update`). Migration nova continua sendo gerada à mão (`migrations add`); o teste `Model_IsInSyncWithMigrations` avisa se esquecer.
