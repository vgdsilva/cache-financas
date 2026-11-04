# CacheFinancas

App pessoal (uso individual) em .NET 10 / ASP.NET Core MVC (Controllers + Views Razor) + HTMX, rodando em homeserver (Docker) e usado no iPhone como PWA.
Dois modos com as MESMAS regras de negócio: telas de CRUD (operacional) e chat em linguagem natural (IA via tool use).

Idioma: comentários, README e respostas em pt-BR. Código, nomes de classes e commits em inglês, **exceto as telas**: controllers de tela, pastas de `Views/` e arquivos `.cshtml` em pt-BR, sem acento (ver "Rotas das páginas").

## Regra de ouro

**Controller e IA (tools) só chamam Handlers de Command/Query. Regra de negócio só nos Handlers (em `Features/`).**
Nenhuma regra em controller, view model, endpoint, tool, DbContext ou código de `Framework/`.

## Arquitetura: Vertical Slice + CQRS

Cada caso de uso é um arquivo em `Features/<Feature>/` com o Command ou Query (um `record`) e o seu handler:

```csharp
public sealed record GrantMenuAccessCommand(Guid UserId, string MenuCode) : ICommand;
public sealed class GrantMenuAccessHandler(...) : ICommandHandler<GrantMenuAccessCommand> { ... }
```

- `ICommand` / `ICommand<TResult>`: mudam estado. `IQuery<TResult>`: só leem, nunca alteram estado.
- Interfaces em `Framework/Cqrs`. **Sem biblioteca de mediator**: o controller ou a tool injeta o handler que precisa (`ICommandHandler<...>` / `IQueryHandler<...>`).
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
  Controllers/              # MVC: camada fina de UI, só chamam handlers (um controller por menu)
  ViewModels/               # modelos das views (campos do formulário, listas); sem regra de negócio
  Views/                    # .cshtml: <Controller>/<Action>.cshtml, Shared/ (layouts, parciais, view components)
  ViewComponents/           # AppNav (topbar mobile + sidebar desktop, só menus liberados), NavIcons
  wwwroot/                  # css, js, manifest, service worker
tests/VGS.FinanceIA.Tests/  # espelha src/ (xUnit + Testcontainers para Postgres)
deploy/                     # Dockerfile, docker-compose.yml, .env.example
```

Previsto: `Ai/` (ChatService, tools). Domínio: controle de finanças (contas, categorias, lançamentos).
Feature nova = pasta em `Features/` + (se precisar de tela) controller em `Controllers/` + views em `Views/` + (se precisar de menu) entrada em `Data/Catalog/Menus.cs`.

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
- **Telas em pt-BR**: um controller por menu, com o nome do menu sem acento (`UsuariosController`, `ContasController`, `MinhaCarteiraController`, `CacauIAController`), e as views em `Views/<Controller>/` (`Views/Usuarios/Index.cshtml`). Ações de tela também em pt-BR (`Criar`, `Editar`, `Excluir`), mas a **rota** da ação continua em inglês (`create`, `edit/{id}`, `delete/{id}`). Todo o resto do C# (handlers, entidades, view models) segue em inglês. **Roteamento por atributo, sempre explícito**: `[Route("configuracoes/usuarios")]` no controller e `[HttpGet("edit/{id:guid}")]` na action. Não dependa do nome do controller para formar a URL, e não crie rota convencional.
- A URL do menu fica em `Data/Catalog/Menus.cs` e é a fonte da verdade; o seeder regrava a URL dos menus existentes na subida. Menu sem URL é só agrupador (título de seção na sidebar).
- Todo controller sob a URL de um menu declara `[RequireMenu(Menus.X)]` desse mesmo menu, e toda action que exige um menu vive sob a URL dele. Os testes `Menus_*` e `PageRoutesTests` garantem isso.
- Fora dessa regra (não são menu): `/Login`, `/Logout`, `/AccessDenied`, `/Error`, `/health`.

## Cadastro de usuários (`/configuracoes/usuarios`)

- `UsuariosController` (`Controllers/`) e views em `Views/Usuarios/`: `Index` (tabela do padrão de CRUD com filtro por coluna e paginação no navegador), `Criar` (`/create`), `Editar` (`/edit/{id}`), `Senha` (`/password/{id}`, redefinir senha) e `Excluir` (`/delete/{id}`, confirmação). `Criar` e `Editar` dividem o `_Formulario` (modelo `UsuarioFormViewModel`). O corpo do POST chega em `UserInput`/`PasswordInput` com `[Bind(Prefix = "Input")]`, e os view models ficam em `ViewModels/`.
- Handlers em `Features/Users` (`ListUsers`, `GetUser`, `RegisterUser`, `UpdateUser`, `ChangeUserPassword`, `DeleteUser`) e `Features/Permissions` (`SetUserMenus`, `ListAssignableMenus`). `CreateUser` é o bloco básico, **sem** checar quem pede (usado pelo seed); a tela usa `RegisterUser`.
- **Só administrador gerencia usuários** (criar, editar, redefinir senha, excluir): imposto nos handlers por `UserRules.RequireAdminAsync`. As páginas só escondem botões/redirecionam para `/AccessDenied`. Motivo: usuário comum com o menu "Usuários" poderia criar admins ou conceder menus que não tem.
- Proteções: ninguém exclui a si mesmo; sempre sobra pelo menos um administrador (ao excluir ou ao tirar o perfil de admin). Excluir é lógico e leva as permissões e configurações do usuário. Nome liberado pode ser reutilizado.
- Administrador acessa todos os menus, então a escolha de menus só vale para usuário comum (fica desabilitada no formulário e é ignorada pelos handlers).
- Só os menus que são páginas (com URL) são atribuíveis; `SetUserMenus` concede/revoga para ficar exatamente igual ao conjunto enviado e invalida o cache de permissões.
- Erro de regra de negócio = `BusinessRuleException` (`Framework/Cqrs`), com mensagem para o usuário final. A UI captura **só** essa (via `TryRunAsync` do controller) e mostra no formulário; qualquer outra exceção é bug e segue como erro. Mensagem de sucesso vai em `TempData["Success"]` (o `_Layout` exibe).
- **Campos do usuário** (`users`): `UserName` (login), `Email`, `FullName`, `PasswordHash`, `IsAdmin`.
  - **Usuário (login)**: só letras, dígitos, ponto, hífen e underscore, 3 a 40 caracteres; gravado em minúsculas; único entre os ativos.
  - **E-mail**: obrigatório e com formato válido; gravado em minúsculas. Não é único (o login é o usuário). Usuários anteriores ao campo ficam com e-mail vazio até a próxima edição, que o exige.
  - **Nome completo**: opcional (até 120); quando existe aparece no cabeçalho e nas listas no lugar do login (`DisplayName`).
  - As regras de formato ficam em `Features/Users/UserRules`. Os handlers apontam o campo no `BusinessRuleException(message, field)` e a tela mostra a mensagem junto do campo (chave `Input.<Campo>` do `ModelState`).
- **Formulário**: seção **Dados do usuário** (usuário, e-mail, nome completo, senha ao criar), seção **Perfil** com a chave (switch) **Administrador** ("Acesso total a todos os menus do sistema") e seção **Menus de Acesso** logo abaixo do perfil, em cartões por grupo, com contagem `(marcados/total)` e links MARCAR TUDO/LIMPAR/Marcar por grupo (JS, aprimoramento progressivo). A chave desabilita a seção de menus.

## Padrão visual dos CRUDs (baseado nas telas "versão 4" do CRM)

A referência de layout das **telas de CRUD** (não da navegação) são as telas "versão 4" do CRM de trabalho: `PVPedido` (`D:\CloverCRM\RC6.0.15\web\CRM\web\PVPedido\PVPedido.aspx`), `FluxoDeAtividades` (`web\Atendimento2\FluxoDeAtividades`) e `OrdemDeServico` (`web\Cadastros\OrdemDeServico`). O CSS delas está em `App_Themes\CRM\stylesheets\css\coreversao4.css` (classes `btn-versao4*`, `versao4-widget`, `versao4-menustopo-menu`, `jajm-titulo`, `form-group-versao4`, `dc-grid`, `dc-modal`) e os componentes Vue em `web\Cadastros\Shared\Scripts\componentes.js`. Seguimos o visual **à risca**: estrutura, medidas, estilo, componentes, **paleta e fontes do CRM** (ver "Visual do CRM" abaixo). (O padrão "v5/Figma" do CRM, com cabeçalho fixo e seções em caixa cinza, foi usado antes e **descartado** a pedido do usuário.)

- **Barra do topo** (`.page-head`, não fixa): título à esquerda (`.page-title`, 16px na cor da marca) e botões à direita (`.page-actions`, 5px entre eles). Listagem: **Adicionar** (primário) + Editar / Redefinir senha / Remover, que agem sobre a **linha selecionada** na tabela (clique seleciona, duplo clique ou Enter abre a edição; botões desabilitados sem seleção ou sem permissão sobre a linha). Formulário: o topo tem só o título; as ações ficam **na parte inferior direita** da tela, abaixo do(s) widget(s), em `.form-actions`: **Cancelar** (cinza, ícone desfazer) e **Salvar** (primário, ícone salvar), esse fora do `<form>` com `form="id-do-form"`. Mobile: layout mobile-first, a definir.
- **Widget** (`.widget`): caixa branca, cantos 5, padding 15, **faixa de 6px embaixo** na cor suave da marca (`--brand-soft`). Todo o conteúdo da tela vai dentro dele.
- **Abas** (`.tabs` > `.tab`, `[data-tabs]` em `site.js`): barra cheia, abas lado a lado com divisória branca, cinza cheio; a ativa fica na cor suave da marca. Sem JS a barra some e todas as partes aparecem em sequência. Abre na primeira aba que tiver campo com erro. Quando a tela tem mais de um assunto (ex.: usuário + menus de acesso).
- **Seção** (`.section`): título 14px na cor da marca com **linha de 1px embaixo** (`.section-head`/`.section-title`), sem caixa em volta. Ações da seção ficam à direita do título como **link de texto** (`.text-action`: 11px, CAIXA ALTA, ícone 12px), ex.: "⟳ ATUALIZAR".
- **Campos** — vêm do **modelo antigo do CRM** (os formulários/modais Bootstrap 3 + tema Clover, p. ex. Agenda e Atendimento), **só os campos e seus estilos**; a estrutura da tela continua sendo uma tela para a grade e outra para o formulário, sem modal. Estilo: rótulo **12px em negrito `#6f776d`** em cima, campo de **28px, sem cantos arredondados**, borda 1px cinza-oliva `rgba(111,119,109,.8)`, valor 13px `#555`, foco = borda verde `rgba(91,175,0,.7)` (sem anel), desabilitado/somente leitura = fundo `#eee`, erro = borda vermelha + mensagem de 11px, ajuda de 10px; 15px entre campos. Obrigatório = rótulo termina com ` *`. Grade `.field-grid` de 12 colunas como o Bootstrap do CRM (**30px entre colunas**; `col-3`/`col-4`/`col-6`/`col-12`). No toque (`pointer: coarse`) ou < 768px o campo sobe para 40px/16px (evita o zoom do iOS).
- **Campos de formulário (marcação)** — tudo dentro de `<div class="field col-4">` com `<label for>`; JS em `wwwroot/js/fields.js` (puro, sem biblioteca; sem JS o campo nativo continua enviando o mesmo valor):
  - **Texto/senha/e-mail**: `<input asp-for=...>`; **textarea**: `<textarea>` (80px, sem redimensionar).
  - **Dinheiro**: `<input type="text" data-money name="X" value="1234.56">`. Mostra `R$ 1.234,50` (digita-se da direita para a esquerda, como o autoNumeric do CRM) e envia o decimal com ponto (`1234.50`) num `<input hidden>` que herda o `name`. Máx. 14 dígitos; só valores positivos.
  - **Data**: `<input type="date" data-date name="X" value="aaaa-mm-dd">`. Vira campo com máscara `dd/mm/aaaa` e ícone de calendário (abre o calendário do aparelho); o `<input type="date">` original fica escondido e envia `aaaa-mm-dd` (liga bem com `DateOnly`). Data inexistente = campo marcado (`aria-invalid`) e nenhum valor enviado.
  - **Select**: `<select>` nativo com seta própria; com **`data-search`** vira lista com busca (sem acento nem caixa; setas, Enter, Esc), e o `<select>` segue no formulário. Select múltiplo com fichas (select2 do CRM) ainda não existe.
  - **Checkbox e radio**: quadrado/redondo de 18px, borda cinza-oliva, marcado = verde `#5baf00` com ✓ (iCheck do CRM). Linha: `<label class="check-line"><input type="checkbox"> <span>Texto</span></label>`; com descrição use `.check-line--top` + `.check-line-text` (`.check-title` e `.check-desc`). **Chave (switch)**: o CRM não tem; usamos **só** no "Administrador" do cadastro de usuário (decisão do usuário): `<label class="switch"><input type="checkbox" role="switch"> <span class="switch-track"></span> <span class="switch-text"><span class="switch-title">…</span><span class="switch-desc">…</span></span></label>`. Nos demais casos, checkbox.
- **Botões** (`.btn`): **CAIXA ALTA, 11px, cantos 5, 30px de altura, mínimo 120**, ícone 12px à esquerda. Primário = cor da marca; secundário e os demais = cinza cheio (`--neutral-solid`), texto branco; hover = opacidade .7. `.btn-danger` (vermelho) só dentro da confirmação de exclusão. Botão quadrado 28×28 (`.icon-button`) só para ações dentro de uma grade editável.
- **Tabela** (`.data-grid`, dc-grid): títulos soltos (12px, sem fundo) → linha de filtros com **campos de 28** e o ícone do tipo (lupa = texto, "=" = número/data, três traços = lista) → linhas de dados de 28 com linha de 1px embaixo; hover/selecionada com tom da marca; tudo alinhado à esquerda. Rodapé "Exibindo 1–N de M registros" + paginação de quadradinhos 28×28 colados, a atual preenchida. Filtros, paginação e seleção rodam no navegador (`[data-grid]` em `site.js`; sem JS a lista aparece inteira e a primeira coluna é um link para editar). **Limitação:** serve para listas curtas; Lançamentos (muitos registros) vai precisar de filtro/paginação no servidor.
- **Confirmação de exclusão**: tela própria (`Excluir`), widget com seção "Confirmação" e botões à direita (Cancelar, Excluir em vermelho). No CRM é um `dc-modal` (cabeçalho colorido centralizado, rodapé com faixa de 5px); virar modal de verdade fica para depois.
- **Ícones**: Font Awesome 5 Free (só a família sólida, `wwwroot/fonts/fa-solid-900.woff2`), como no CRM (`fa-plus`, `fa-edit`, `fa-times`, `fa-save`, `fa-undo`...). Uso: `<partial name="_Icone" model='"editar"' />` (`Views/Shared/_Icone.cshtml`). Ícone novo = glifo `.fa-xxx::before` em `site.css` (código do glifo no Font Awesome) + um nome novo no `switch` do `_Icone`. Remover = X; adicionar = "+". Os ícones da sidebar são outro conjunto (`NavIcons`, SVG).
- Tudo isso vive sob `.app-main` em `wwwroot/css/site.css` (o login não é afetado). Medidas são variáveis em `:root` (`--control-h`, `--button-h`, `--row-h`, `--icon-button`, `--space-*`, `--radius-*`); cor nova = token em `:root`.
- Ainda **não implementados** (existem no CRM, entram quando uma tela precisar): modal de verdade (`dc-modal`), select múltiplo com fichas, campo de hora, grade editável (+ / lixeira por linha), registro expansível (acordeão), cartão de resumo da entidade.

## Layout e navegação

- CSS e JS próprios (`wwwroot/css/site.css`, `wwwroot/js/site.js`), sem Bootstrap/Tailwind e sem Node. Cores e medidas são variáveis em `:root`.
- **Visual do CRM** (decisão do usuário: seguir à risca, **só tema claro**, sem modo escuro): paleta do `theme_clover.css`/`coreversao4.css` — verde `#4f853d` (marca: títulos, botão de destaque, links), `#a2c652` (aba ativa, foco do campo, faixa do widget, página atual), `#5baf00` (item ativo da sidebar), cinza-oliva `#6f776d` (sidebar), `rgba(111,119,109,.7)` (botão secundário e abas), fundo da página `rgba(111,119,109,.15)`, texto `#333`/`#474747`, perigo `#c0392b`. Fontes: **Open Sans** (corpo; Regular e Bold em `wwwroot/fonts`, licença Apache 2.0) e **Trebuchet MS** (rótulos, títulos de seção, abas e cabeçalho da tabela; é a fonte do sistema, não é distribuída aqui, e cai para a Open Sans onde não existir). O botão de destaque usa Arial, como no CRM. Cor nova = token em `:root` com um valor só; não use cor fixa nos componentes.
- **O shell logado reproduz a estrutura do layout de referência** (app Next/Tailwind do trabalho): sidebar de 18rem com cabeçalho (marca + recolher/fechar), seções com título em caixa-alta de 10px, item ativo preenchido; header de 3.5rem com hambúrguer (só mobile), atalho do chat e menu do usuário (avatar, nome, perfil, dropdown com Sair); conteúdo rola dentro de `<main>`. As medidas são as do Tailwind; o breakpoint é o `md` (768px). As **cores** são as do CRM: sidebar cinza-oliva com texto branco e item ativo `#5baf00`, header branco com sombra interna de 2px embaixo (como a `crm-bar`).
- Mobile (< 768px): a sidebar é uma **gaveta** que desliza da esquerda (hambúrguer no header, X no cabeçalho da sidebar, fundo escurecido, Esc fecha). Desktop (≥ 768px): sidebar fixa, recolhível só com ícones (estado em `localStorage`, aplicado no `<head>` para não piscar).
- Sidebar = `AppNav` (menus liberados); header = `AppHeader` (perfil via `GetUserProfileQuery`, só exibição). Toda seção tem título: menus de primeiro nível ficam em "Principal".
- Não temos as abas de domínio nem o sino de notificações da referência (não há o que mostrar); entram quando houver conteúdo real.
- `_Layout` mostra o shell só para usuário autenticado; `_AuthLayout` é o da tela de login, que segue o **login do CRM (Clover 6)**: marca à esquerda (no CRM é um carrossel de imagens; some abaixo de 1024px) e formulário à direita, centro de 22,5vw (cartão de login: exceção à regra de largura total), título "Seja bem-vindo(a)" em Nunito 700 24px, rótulos com `*` vermelho, campos de 40px com borda cinza e foco `#5baf00`, olho de mostrar/ocultar a senha, botão `Entrar` verde `#5baf00` (hover `#98de00`, foco `#498b00`), erro por campo (`.has-error`: rótulo/borda vermelhos + mensagem com ícone), erro geral em `.mensagem-feedback`, rodapé fixo de 50vw e tela de carregando ao enviar. Fontes **Quicksand** e **Nunito** (OFL, em `wwwroot/fonts`), paleta própria do login (`--lg-*` em `.auth`). Diferenças deliberadas: botão Entrar não nasce desabilitado (o gerenciador de senhas do iPhone preenche sem disparar evento) e, no celular, o cartão usa a largura da tela menos a margem. Sem "esqueceu a senha" (não há fluxo).
- A sidebar é montada a partir de `GetAllowedMenusQuery`: menu **sem URL** é agrupador (título de seção) e os filhos liberados ficam abaixo; os demais vão na seção "Principal". O ícone vem do campo `Icon` do catálogo e é resolvido em `NavIcons` (nome desconhecido usa um ícone padrão; ícone novo = entrada em `NavIcons`).
- **Só tema claro**: `color-scheme: light` em `:root` e `<meta name="color-scheme" content="light">`; não existe seletor de tema nem `data-theme`. O script inline de `_Head.cshtml` só aplica o estado "recolhido" da sidebar (`localStorage` `nav-collapsed`) antes da primeira pintura (precisa vir antes do CSS).
- **Páginas usam a largura inteira da tela**: sem `max-width` em página, formulário, tabela ou cartão (o conteúdo preenche o `<main>`). Só são aceitos limites que não são de largura de página: a gaveta do menu no mobile (`86vw`), o truncamento do nome no header e o cartão de login (tela centralizada própria).
- **MVC não valida antiforgery sozinho**: o filtro `AutoValidateAntiforgeryTokenAttribute` está registrado globalmente em `Program.cs`, então todo POST precisa do token (o tag helper `<form method="post">` o emite; um POST feito por HTMX precisa enviar o token no header `RequestVerificationToken`).
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
- O catálogo de menus fica em `Data/Catalog/Menus.cs` (códigos estáveis, ex.: `REG.TRANSACTIONS`, filho do grupo `REG`). Menu novo = constante + definição lá (pais antes dos filhos); o `DatabaseSeeder` cria a linha na próxima inicialização, acompanha texto/ícone/URL/ordem/pai dos existentes e **desativa** (sem apagar) o que saiu do catálogo. Estrutura atual: Principal (Painel Financeiro, Visão Geral da Carteira, Cacau IA = chat), Cadastros (Contas, Lançamentos, Categorias), Configurações (Usuários).
- A pergunta "quem acessa o quê" é respondida só por `HasMenuAccessQuery`. As permissões **não** vão no cookie (só id e nome): revogar vale na hora.
- Toda página declara o menu que exige: `[RequireMenu(Menus.Transactions)]` no controller (cobre as actions HTMX do mesmo controller). Sem atributo, a página exige apenas usuário autenticado; `[AllowAnonymous]` só para Login, AccessDenied e Error.
- Toda tool de IA declara o menu que exige (`RequiredMenu`); o `ToolRegistry` só oferece ao modelo as tools permitidas e revalida (com `HasMenuAccessQuery`) antes de executar.
- Admin inicial: `Seed:AdminUserName`, `Seed:AdminPassword` e `Seed:AdminEmail` (variáveis `Seed__AdminUserName`, `Seed__AdminPassword` e `Seed__AdminEmail`), criado só se não houver usuários. Senha com 8+ caracteres por padrão; `Auth:MinPasswordLength` reduz o mínimo e **só deve ser usado em desenvolvimento** (o `appsettings.Development.json` usa `admin`/`123`, e-mail `admin@cachefinancas.local`, com mínimo 3; nunca coloque isso em Production).
- Na subida, o `DatabaseInitializer` aplica as **migrations pendentes** e depois roda o seeder. Desligue ambos com `Database:InitializeOnStartup=false` (aí o schema é aplicado por fora, com `dotnet ef database update`). Migration nova continua sendo gerada à mão (`migrations add`); o teste `Model_IsInSyncWithMigrations` avisa se esquecer.
