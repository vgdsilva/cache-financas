# VGS.FinanceIA

App pessoal (uso individual) em .NET 10 / ASP.NET Core Razor Pages + HTMX, rodando em homeserver (Docker) e usado no iPhone como PWA.
Dois modos com as MESMAS regras de negócio: telas de CRUD (operacional) e chat em linguagem natural (IA via tool use).

Idioma: comentários, README e respostas em pt-BR. Código, nomes de classes e commits em inglês.

## Regra de ouro

**PageModel e IA (tools) só chamam Services. Regra de negócio só nos Services.**
Nenhuma regra em PageModel, endpoint, tool ou DbContext.

## Estrutura

```
src/                        # UM único projeto web (VGS.FinanceIA.csproj fica direto aqui)
  Program.cs                # DI, auth, /health, endpoint de chat (SSE)
  Pages/                    # Razor Pages + partials HTMX
  wwwroot/                  # css, js, manifest, service worker
tests/VGS.FinanceIA.Tests/  # xUnit (+ Testcontainers para Postgres)
deploy/                     # Dockerfile, docker-compose.yml, .env.example
```

Pastas previstas em `src/` (criadas conforme as etapas): `Domain/`, `Data/` (DbContext + Migrations), `Services/`, `Ai/` (ChatService, tools).

## Convenções

- `net10.0`, C# 14, `Nullable` enabled, `TreatWarningsAsErrors` (config em `Directory.Build.props`).
- Namespaces file-scoped; campos privados `_camelCase` (ver `.editorconfig`).
- HTMX para interatividade; sem SPA, sem Blazor, sem Node no build.
- Minimal APIs apenas para chat (SSE) e `/health`.
- Modelo de IA configurável via tabela de configurações no banco; `ANTHROPIC_API_KEY` vem do ambiente.
- Commits pequenos e semânticos (`feat:`, `fix:`, `chore:`, `test:`, `docs:`), em inglês. Trabalho direto na `develop`.

## Comandos

```
dotnet build
dotnet test          # testes de integração exigem Docker (Testcontainers)
```
