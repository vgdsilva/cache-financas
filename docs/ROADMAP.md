# Roadmap

> **Rascunho de 27/10/2026.** Planejado do zero a partir da [visão do produto](PRODUCT.md). O código que já existe entra só onde serve, marcado como **Reaproveita**. O estado atual está no [mapa do projeto](mapa-do-projeto.excalidraw).

## Como ler

- As fases estão em ordem. Cada uma termina com algo usável e tem um critério de pronto.
- Cada task tem um ID (`F2.3`) para virar issue e branch depois.
- Decisões (`D1`…) precisam de resposta antes das tasks que dependem delas.
- A partir da Fase 2, toda tela nova cadastra o próprio menu e respeita a permissão por ação.

## Decisões

| # | Decisão | Opções | Recomendação |
|---|---|---|---|
| D1 | Front | Blazor WebAssembly (já em uso) · React + Vite | Manter Blazor WASM: o login já funciona e os DTOs são compartilhados com a API. O download do runtime pesa só na primeira abertura; depois o service worker serve do cache (medir na F3.4). |
| D2 | Acesso de fora de casa | Tailscale serve (cada pessoa instala o Tailscale) · Tailscale Funnel (URL pública) · Caddy + domínio próprio | Esposa e amigos sem Tailscale precisam de Funnel ou domínio próprio. Qualquer opção pública só depois da Fase 2. |
| D3 | Versão do .NET | .NET 11 (RC hoje) · .NET 10 LTS | Ficar no .NET 11 e subir para a versão final quando sair (prevista para novembro de 2026). Voltar para o 10 é retrabalho. |
| D4 | Modelo de permissões | Só Admin/Usuário · permissão por menu (`MenuItem` + `UserMenuPermission`) | **Decidido:** permissão por menu e ação, com três perfis (Root, Admin, Usuário). Ver Fase 2. |
| D5 | Cartão de crédito no MVP | Sim (fatura, fechamento, vencimento) · depois | Sim: boa parte dos gastos passa pelo cartão, e sem fatura o orçamento do mês fica errado. |
| D6 | Nome do produto | Cache Finanças · FMS · outro | — |
| D7 | O console do root alcança as tabelas financeiras? | Não: role do banco sem acesso a elas · Sim: acesso a tudo, com auditoria | **Decidido:** sim. O console do root consulta qualquer tabela do banco; é a única exceção ao isolamento e fica registrado na auditoria. |

## O que reaproveitar do código atual

| Parte | Destino |
|---|---|
| Deploy (compose, nginx, Tailscale, GitHub Actions) | Mantém; corrige na Fase 1 |
| Estrutura da solução, `Result<T>`, `ICommandHandler`/`IQueryHandler` | Mantém |
| `User`, `UserSession`, `UserLoginAttempt`, login JWT com bloqueio | Mantém; completa na Fase 2 |
| `MenuItem`, `UserMenuPermission` e a branch `feature/login` | Mantém; base dos perfis e da permissão por menu (Fase 2) |
| Tela de login e `FmsAuthenticationStateProvider` | Mantém; muda onde o token fica guardado (F2.2) |
| Páginas do template (`Counter`, `Weather`) | Remove (F1.9) |

---

## Fase 1: fundação

**Objetivo:** a `develop` atual rodando em produção, com CI, testes, logs e backup. Nenhuma funcionalidade nova.
**Reaproveita:** compose, nginx, Tailscale, estrutura da solução.

- [ ] **F1.1** Fechar D1, D2, D3 e D6 e atualizar o `ARQUITECTURE.md` (hoje ele fala em Caddy, front "Em aberto" e .NET 10).
- [ ] **F1.2** `Dockerfile.server`: copiar todos os projetos referenciados (faltam `FMS.Communication` e `FMS.Web.Api.Bussiness.Rules`).
- [ ] **F1.3** `deploy.yml`: gravar `JWT_SECRET` no `deploy/.env` a partir de um novo secret do GitHub.
- [ ] **F1.4** CI em toda PR para `develop` e `main`: build e testes. O deploy só roda se passar.
- [ ] **F1.5** Projetos de teste na solução: unitário e integração com PostgreSQL real em container (Testcontainers).
- [ ] **F1.6** Migrations: passo separado no deploy ou startup que falha de forma explícita. Hoje o erro é engolido e a API sobe com o schema errado.
- [ ] **F1.7** Serilog + Seq no compose, sem porta publicada.
- [ ] **F1.8** Backup diário: `pg_dump` criptografado enviado para fora do servidor, com restauração testada e documentada.
- [ ] **F1.9** Limpeza: remover `Counter`, `Weather` e `weather.json`; corrigir os links do README; renomear `Bussiness` para `Business` enquanto é barato.
- [ ] **F1.10** Merge da `develop` na `main` com deploy verde.

**Pronto quando:** um push na `main` compila, testa, faz deploy e passa no health check, e existe um backup que já foi restaurado com sucesso.

## Fase 2: autenticação, gestão de usuários e isolamento

**Objetivo:** cada pessoa entra com a própria conta e vê só os menus liberados para ela. Nas telas do app ninguém, nem o admin, vê os dados financeiros de outra pessoa; a única exceção é o console de consultas do root, que é auditado.
**Reaproveita:** tela de login, entidades de usuário, sessão, menu e permissão, `LoginCommandHandler`, `JwtTokenService`; da `feature/login`, roles, menus no JWT, `MenuPolicyProvider` e seed.

### Perfis

| Perfil | Quem | O que pode |
|---|---|---|
| Usuário | esposa, amigos | as próprias finanças, nos menus liberados para ele |
| Admin | o autor e quem ele promover | tudo o que um usuário pode, com as próprias finanças isoladas como as de qualquer um, mais a gestão de usuários e promover usuários a admin |
| Root | conta de sistema, uma só | consultas no banco, logs técnicos brutos, gestão de usuários e estruturas do sistema (F2.15–F2.17); não tem finanças próprias |

### Login e sessão

- [ ] **F2.1** Tela de login (reaproveita `Login.razor` e `AuthService`): entra por login ou e-mail, mostra o bloqueio temporário e leva para a troca de senha quando `MustChangePassword`.
- [ ] **F2.2** Refresh com rotação (`POST /auth/refresh`). Refresh token em cookie HttpOnly (web e API têm a mesma origem via nginx) e access token só em memória, fora do `localStorage`.
- [ ] **F2.3** Logout que revoga a `UserSession`, e validação do `sid`: sessão revogada deixa de valer.
- [ ] **F2.4** Troca de senha obrigatória no primeiro acesso e troca de senha pelo próprio usuário.
- [ ] **F2.5** 2FA por TOTP (os campos já existem), com códigos de recuperação.
- [ ] **F2.6** Rate limiting no login por IP (o bloqueio atual é por usuário e não segura tentativas em vários logins).

### Perfis, menus e permissões

- [ ] **F2.7** Partir da `feature/login`: revisar, mergear e trocar `IsAdmin` por `Perfil` (Root, Admin, Usuário), enviado no claim `role` do JWT.
- [ ] **F2.8** Estrutura de menus: `MenuItem` em árvore com código estável (ex.: `FIN.LANCAMENTOS`), seed dos menus e marcação dos menus exclusivos do root, que nunca podem ser liberados para outro perfil.
- [ ] **F2.9** Permissão por menu e ação (ver, criar, editar, excluir) em `UserMenuPermission`. Os menus do usuário chegam no login, o `NavMenu` é montado a partir deles e cada página exige a policy do próprio menu.
- [ ] **F2.10** Autorização na API pelo código do menu e pela ação. Esconder no front não basta: sem permissão, a API responde 403 mesmo quando chamada direto.

### Gestão de usuários

- [ ] **F2.11** Tela de gestão de usuários para admin e root: listar, criar, editar, desativar, resetar senha, encerrar sessões e marcar as permissões por menu.
- [ ] **F2.12** Promoção: o root e qualquer admin podem promover um usuário a admin. Rebaixar ou desativar um admin fica só com o root (a confirmar). Ninguém promove a root, e o root não pode ser alterado, desativado nem excluído pela aplicação.
- [ ] **F2.13** Root único garantido no banco: índice único parcial em `Users` para o perfil Root, de modo que o PostgreSQL recusa um segundo root mesmo fora da aplicação.
- [ ] **F2.14** Seed cria o root com credenciais vindas de secret e 2FA obrigatório; o root promove o primeiro admin.

### Root

- [ ] **F2.15** Console de consultas no banco, só para o root: consulta qualquer tabela (D7), em modo só leitura (role do PostgreSQL só com `SELECT`), com timeout e limite de linhas, e toda consulta registrada na auditoria com o texto da consulta.
- [ ] **F2.16** Logs técnicos brutos: acesso ao Seq (F1.7) só para o root, direto ou por uma tela que lê dele.
- [ ] **F2.17** Estruturas do sistema: cadastro de menus, auditoria, sessões ativas e tentativas de login de todos, e saúde do sistema.

### Isolamento e auditoria

- [ ] **F2.18** Isolamento por dono: base para entidades financeiras com `OwnerUserId` e filtro global pelo usuário logado. Vale para o admin como para qualquer usuário. Testes de integração provam que A não lê nem altera dados de B.
- [ ] **F2.19** Auditoria de segurança: logins, falhas, trocas de senha, mudanças de perfil e de permissão, consultas do root.
- [ ] **F2.20** Liberar o acesso externo escolhido na D2.

**Pronto quando:** existe um único root (o banco recusa um segundo); o root promove o admin; o admin cria a conta da esposa com só alguns menus liberados; ela não vê nem consegue chamar pela API o que não foi liberado; e o teste de isolamento passa no CI.

## Fase 3: app instalável (PWA mobile-first)

**Objetivo:** abrir pelo ícone na tela de início do iPhone com cara de app, antes de existir qualquer tela financeira.

- [ ] **F3.1** Manifest (nome, ícones, `display: standalone`, cores) e service worker com cache do app.
- [ ] **F3.2** Layout mobile-first: navegação inferior, áreas seguras (notch e barra inferior), tema claro e escuro.
- [ ] **F3.3** Componentes base: campo de valor em R$, seletor de data, listas, estados de vazio, carregando e erro.
- [ ] **F3.4** Medir a primeira abertura e as seguintes no iPhone. Confirma ou derruba a D1.
- [ ] **F3.5** Tela de perfil: dados, senha, 2FA e sessões ativas.

**Pronto quando:** instalado no iPhone, abre em tela cheia, e a segunda abertura não baixa o runtime de novo.

## Fase 4: lançamentos (MVP de uso diário)

**Objetivo:** substituir o que vocês usam hoje para anotar gastos.

- [ ] **F4.1** Modelo: Conta (corrente, poupança, dinheiro, cartão de crédito), Categoria (padrão e do usuário, com subcategoria) e Transação (receita, despesa, transferência).
- [ ] **F4.2** Serviço único de criação de transações, usado pelas telas, pelo chat e pelas importações.
- [ ] **F4.3** Lançamento rápido: valor, categoria e conta; data e descrição opcionais. Meta: menos de 10 segundos no celular.
- [ ] **F4.4** Extrato do mês com filtros (conta, categoria, tipo), edição e exclusão.
- [ ] **F4.5** Saldo por conta e saldo total.
- [ ] **F4.6** Cartão de crédito: fechamento, vencimento, fatura do mês e pagamento da fatura como transferência (se D5 = sim).
- [ ] **F4.7** Hangfire com storage no PostgreSQL, dentro da API.
- [ ] **F4.8** Parcelamentos e recorrências (aluguel, assinaturas), gerados por job.
- [ ] **F4.9** Auditoria de negócio: quem criou, alterou ou excluiu cada transação, e quando.
- [ ] **F4.10** Contas e categorias sugeridas no primeiro acesso.

**Pronto quando:** um mês inteiro de gastos foi lançado só pelo app.

## Fase 5: orçamento e visão do mês

**Objetivo:** saber, no fim do mês, quanto sobrou e onde estourou.

- [ ] **F5.1** Orçamento mensal por categoria, com opção de copiar do mês anterior.
- [ ] **F5.2** Gasto x orçado por categoria, com progresso.
- [ ] **F5.3** Resumo do mês: receitas, despesas e quanto sobrou.
- [ ] **F5.4** Gráficos: despesas por categoria e evolução dos últimos meses.
- [ ] **F5.5** Notificações push (iOS 16.4+, app instalado): orçamento perto do limite e fatura vencendo.

**Pronto quando:** "quanto sobrou e onde estourou" se responde só pelo app.

## Fase 6: lançamento por chat com IA

**Objetivo:** lançar gastos escrevendo em linguagem natural, sem nada gravado em silêncio.

- [ ] **F6.1** Escolher provedor e modelo: custo mensal para o grupo e que dados saem do servidor.
- [ ] **F6.2** Microsoft.Extensions.AI com o provedor escolhido; chave só no servidor.
- [ ] **F6.3** Texto vira proposta de transação (valor, categoria, conta, data) usando as contas e categorias do usuário.
- [ ] **F6.4** Confirmação obrigatória antes de gravar, sempre pelo serviço da F4.2.
- [ ] **F6.5** Resposta em streaming via SSE.
- [ ] **F6.6** Histórico de conversas e registro de quais transações vieram do chat.
- [ ] **F6.7** Limites de uso por usuário, timeout e volta para o formulário em caso de falha.

**Pronto quando:** "gastei 32 no mercado no débito" vira a transação certa com um toque de confirmação.

## Fase 7: investimentos

**Objetivo:** acompanhar a carteira, os aportes, os resgates e a rentabilidade.

- [ ] **F7.1** Definir os tipos de ativo (renda fixa, Tesouro, ações/FIIs, cripto…) e a fonte de cotações.
- [ ] **F7.2** Modelo: Ativo, Posição e Movimento (aporte, resgate, rendimento).
- [ ] **F7.3** Aporte e resgate ligados a uma conta da Fase 4 (o dinheiro sai ou entra na conta).
- [ ] **F7.4** Job diário de cotações, com histórico.
- [ ] **F7.5** Carteira: posição atual, alocação por tipo, rentabilidade por ativo e total.

**Pronto quando:** a posição no app bate com o extrato da corretora.

## Fase 8: metas e planejamento

**Objetivo:** cada meta mostra se está no ritmo e quanto falta por mês.

- [ ] **F8.1** Meta: nome, valor alvo, prazo e investimentos vinculados.
- [ ] **F8.2** Progresso pela posição dos investimentos vinculados.
- [ ] **F8.3** Projeção com a sobra média real (Fase 5) e a rentabilidade (Fase 7): "no ritmo atual, chega em X".
- [ ] **F8.4** Simulação: quanto guardar por mês para chegar no prazo.

**Pronto quando:** cada meta responde "estou no ritmo?" e "quanto preciso guardar por mês?".

## Fase 9: importação automática (futuro)

- [ ] **F9.1** Importar extrato OFX/CSV, com deduplicação e revisão antes de gravar.
- [ ] **F9.2** Avaliar Open Finance via agregador: custo, consentimento e bancos cobertos.
- [ ] **F9.3** Conciliação entre transação importada e lançamento manual.

## Fora do roadmap

Os mesmos itens fora do escopo da [visão do produto](PRODUCT.md): finanças compartilhadas, app nativo, uso comercial e recomendações de investimento.
