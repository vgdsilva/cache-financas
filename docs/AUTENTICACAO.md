# Autenticação e autorização

Especificação da primeira versão. Branch: `feature/autenticacao`.

## Decisões

| Tema | Decisão |
|---|---|
| Usuários | Poucos; **cadastro somente pelo admin** (sem registro público) |
| Isolamento | Cada usuário vê apenas os próprios dados; admin acessa a área de gerenciamento |
| Mecanismo | Login + senha, cookie de autenticação do ASP.NET Core, hash BCrypt |
| Extras v1 | Bloqueio por tentativas (lockout) |
| Fora do escopo v1 | 2FA, recuperação de senha por e-mail, login externo, registro público |

## Modelo de domínio

`Usuario : EntityControl` (`CacheFinancas.Domain/Entities`)

| Campo | Tipo | Observação |
|---|---|---|
| `Nome` | string | obrigatório |
| `Email` | string | único (índice, case-insensitive), usado como login |
| `SenhaHash` | string | BCrypt (work factor ≥ 12) |
| `Perfil` | enum `PerfilUsuario` { `Usuario`, `Admin` } | |
| `Ativo` | bool | desativar impede login sem apagar dados |
| `TentativasFalhas` | int | zera no login com sucesso |
| `BloqueadoAte` | DateTime? (UTC) | lockout |
| `DeveTrocarSenha` | bool | true para usuários criados pelo admin |

Remoção usa o soft delete já existente em `EntityControl` (`SystemDeleted`).

## Fluxos 

### Login
1. `POST /Conta/Login` com e-mail e senha (antiforgery obrigatório).
2. Usuário inexistente, inativo ou excluído → mensagem genérica "E-mail ou senha inválidos" (não revelar qual).
3. Se `BloqueadoAte > agora` → mensagem genérica de bloqueio, sem verificar a senha.
4. Senha incorreta → `TentativasFalhas++`; ao atingir **5**, `BloqueadoAte = agora + 15 min` (configurável).
5. Sucesso → zera tentativas, emite cookie com claims `NameIdentifier` (Id), `Name`, `Email`, `Role`.
6. Se `DeveTrocarSenha`, redireciona para `/Conta/TrocarSenha` antes de qualquer outra página.
7. Para mitigar enumeração por tempo, usar BCrypt contra um hash fictício quando o usuário não existir.

### Logout
`POST /Conta/Logout` (antiforgery), apaga o cookie.

### Troca de senha (usuário logado)
Exige senha atual, nova senha (mín. 10 caracteres), limpa `DeveTrocarSenha`.
Necessária no primeiro acesso, mesmo sem recuperação por e-mail.

### Gestão de usuários (somente Admin)
Área `/Admin/Usuarios`: listar, criar (com senha temporária e `DeveTrocarSenha = true`), ativar/desativar, redefinir senha, desbloquear.
O admin não pode desativar ou rebaixar a si mesmo.

### Bootstrap do primeiro admin
Não existe tela pública. O admin inicial é criado na inicialização se não houver nenhum usuário, a partir de variáveis de ambiente:

- `Bootstrap__AdminEmail`
- `Bootstrap__AdminSenha`

Entram como secrets no GitHub e em `deploy/.env.example`. Após criado, as variáveis são ignoradas, e o admin é forçado a trocar a senha (`DeveTrocarSenha = true`).

## Autorização

- Política global: `FallbackPolicy` exige usuário autenticado; somente `/Conta/Login` e `/health` são anônimos.
- `[Authorize(Roles = "Admin")]` na área administrativa.
- **Isolamento de dados**: toda entidade de negócio terá `UsuarioId`; repositórios/queries filtram sempre pelo usuário do cookie (query filter global do EF), nunca por parâmetro vindo do cliente. O admin **não** vê dados financeiros dos outros, apenas o gerenciamento de contas.

## Segurança

| Item | Configuração |
|---|---|
| Cookie | `HttpOnly`, `Secure` (`SecurePolicy = Always`), `SameSite=Lax`, nome `__Host-cf`, expiração deslizante de 8 h |
| CSRF | `AutoValidateAntiforgeryToken` global |
| HTTPS | Terminado pelo Tailscale; app confia em `X-Forwarded-Proto` (já configurado) |
| Rate limit | `AddRateLimiter` no endpoint de login (por IP), além do lockout por conta |
| Chaves | Data Protection já persistido no volume `dataprotection-keys`; sessões sobrevivem a deploys |
| Logs | Nunca registrar senha ou hash; registrar login, falha e bloqueio com Id/e-mail |
| Headers | `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy` |

IP real: atrás do Funnel, o IP vem de `X-Forwarded-For` (habilitar `ForwardedHeaders` com `XForwardedFor`).

## Estrutura por camada

- **Domain**: `Usuario`, `PerfilUsuario`, regras de lockout (`RegistrarFalha`, `RegistrarSucesso`, `EstaBloqueado`).
- **Application**: `IAutenticacaoService`, `IUsuarioService`, `IPasswordHasher` (abstração do BCrypt), DTOs.
- **Infrastructure**: `AppDbContext`, mapeamento de `Usuario`, `BCryptPasswordHasher`, migrations, seed do admin.
- **Web**: `ContaController`, `AdminUsuariosController`, views de login/troca de senha/gestão, configuração de cookie e políticas.

## Banco

Primeira migration: tabela `usuarios` com índice único em `lower(email)`.
Connection string vem de `ConnectionStrings__Default` (já provisionada no deploy).
Migrations aplicadas no startup somente se `Database__MigrarNoStartup=true` (evita rodar acidentalmente fora do deploy).

## Critérios de aceite

1. Sem cookie, qualquer rota (exceto login e `/health`) redireciona para o login.
2. Cinco senhas erradas bloqueiam a conta por 15 min; o login correto durante o bloqueio também é recusado.
3. Mensagens de erro de login são idênticas para e-mail inexistente e senha errada.
4. Usuário comum recebe 403 em `/Admin/*`.
5. Usuário A não acessa dados do usuário B, nem por Id direto na URL.
6. Usuário criado pelo admin é obrigado a trocar a senha no primeiro login.
7. Reiniciar o container não desloga os usuários.
8. Senhas e hashes não aparecem em logs.

## Plano de implementação

1. Domain: entidade, enum e regras de lockout (com testes unitários).
2. Infrastructure: `DbContext`, mapeamento, migration, `BCryptPasswordHasher`.
3. Application: serviços de autenticação e de usuários.
4. Web: cookie, políticas, `ContaController` e views.
5. Área admin e gestão de usuários.
6. Bootstrap do admin e secrets no workflow/`.env.example`.
7. Endurecimento: rate limit, headers, revisão de logs.
8. Documentar em `DEPLOY.md` os novos secrets e o procedimento do primeiro acesso.

## Decisões fechadas

- Admin **não** acessa dados financeiros de outros usuários; só gerencia contas.
- Política de senha mínima: apenas tamanho mínimo de 10 caracteres, sem regras de complexidade.
- Sessão de 8 h deslizante, sem "lembrar-me".
