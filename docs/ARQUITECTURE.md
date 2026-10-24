# Arquitetura

## Visão geral

```
Usuários (iPhone, tablet, computador)
        │ HTTPS
        ▼
     Caddy ──► API ASP.NET Core ──► PostgreSQL
                    │                   ▲
                    │ Hangfire          │ pg_dump
                    ▼                   │
              APIs externas          Backup ──► armazenamento externo
     (IA, cotações, Open Finance)
```

Tudo roda em Docker Compose numa VPS privada.

## Stack

| Camada | Tecnologia | Status |
|---|---|---|
| Backend | ASP.NET Core (.NET 10, LTS) | Decidido |
| ORM | EF Core com provider Npgsql | Decidido |
| Banco | PostgreSQL | Decidido |
| Jobs em segundo plano | Hangfire com storage no PostgreSQL | Decidido |
| Autenticação | Própria (usuários, sessões e permissões em tabelas), JWT de curta duração com claims + refresh token em `UserSession`, 2FA | Decidido |
| Logs técnicos | Serilog + Seq | Decidido |
| Proxy reverso e HTTPS | Caddy | Decidido |
| Abstração de IA | Microsoft.Extensions.AI | Decidido |
| Front | React + Vite (TypeScript) **ou** Blazor WebAssembly, como PWA | **Em aberto** |

## Containers

| Container | Função | Exposto à internet |
|---|---|---|
| `caddy` | HTTPS automático e proxy reverso | Sim (80 e 443) |
| `api` | API ASP.NET Core e Hangfire | Não, apenas via Caddy |
| `postgres` | Banco de dados, com volume persistente | Não |
| `seq` | Logs técnicos | Não |
| `backup` | Dump diário criptografado enviado para fora do servidor | Não |

No início o Hangfire roda dentro do container da API. Pode virar um worker separado se os jobs crescerem.

## Estrutura da solução

| Projeto | Responsabilidade |
|---|---|
| `Domain` | Entidades e regras de negócio, sem dependências externas |
| `Application` | Casos de uso e serviços. Toda lógica de criação de transações fica aqui, compartilhada entre telas e chat |
| `Infrastructure` | EF Core, PostgreSQL, Hangfire, provedores de IA, integrações externas |
| `Api` | Endpoints, autenticação, autorização, configuração |
| `Tests` | Testes, com prioridade nas regras de domínio |

Regra: controllers e endpoints não contêm lógica de negócio.

## Front e PWA

Independente da tecnologia escolhida, o front é um PWA instalável no iPhone pela opção "Adicionar à Tela de Início" do Safari:

- Manifest com nome, ícones e `display: standalone`.
- Service worker.
- Layout respeitando as áreas seguras (notch e barra inferior).
- Design mobile-first.
- Notificações push disponíveis no iOS 16.4+ para apps instalados.

Blazor Server foi descartado: depende de conexão SignalR permanente, que o iOS derruba quando o app vai para segundo plano.

Comparação das opções em aberto:

| Critério | Blazor WebAssembly | React + Vite |
|---|---|---|
| Linguagem | C# | TypeScript |
| Download inicial | Alguns MB (runtime .NET) | Centenas de KB |
| Abertura no iPhone | Lenta na primeira vez | Praticamente instantânea |
| Contratos com a API | DTOs compartilhados diretamente | Cliente gerado do OpenAPI |
| Ecossistema mobile | Menor | Muito maior |

## Comunicação em tempo real

As respostas do chat com IA chegam em streaming via Server-Sent Events.

## Deploy

1. Push na branch principal.
2. CI compila, roda os testes, gera a imagem Docker e publica num registry.
3. Na VPS, a nova imagem é baixada e o container reiniciado.
4. Migrações do EF Core são aplicadas pela própria API na inicialização (`ApplyMigrationsAsync`). Falhas são registradas em log e não impedem a API de subir.

## Observabilidade

- **Health check:** `/health` responde HTML para navegadores e JSON para ferramentas, mantendo o código HTTP (200 ou 503). Mensagens de erro só aparecem em `Development`.
- **Logs técnicos:** Serilog para o Seq.
- **Auditoria de negócio:** tabela própria no banco (ver [segurança](04-seguranca-e-acesso.md)).

## Backup

- Dump diário do PostgreSQL, criptografado, enviado para armazenamento fora da VPS.
- Teste de restauração periódico.
