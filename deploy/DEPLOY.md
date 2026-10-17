# Deploy — Cache Financas

## Arquitetura

```
push na main ─▶ GitHub Actions ─▶ runner self-hosted (vgdsilva-server)
                                        │ docker compose up -d --build
                                        ▼
 Tailscale (HTTPS) ─▶ tailscale serve ─▶ 127.0.0.1:8080 ─▶ container web (Next.js :8080)
                                                              │ /api/* (rewrite, rede default)
                                                              ▼
                                                        container server (ASP.NET Core :3333)
                                                              │ rede db-net
                                                              ▼
                                                        container postgres (stack separado)
```

- **web** (`deploy/Dockerfile.web`, `src/web`): Next.js em modo `standalone`, porta publicada apenas em `127.0.0.1:8080`. O navegador fala so com o web; `/api/*` e repassado ao server (`/api/health` -> `server:3333/health`), entao nao ha CORS nem cookies cross-origin.
- **server** (`deploy/Dockerfile.server`, `src/server`): ASP.NET Core 10 + EF Core, **sem porta publicada**; so o web (rede default) e o Postgres (`db-net`) o alcancam. No start aplica as migrations do EF Core (`Database__MigrarNoStartup=true`).
- **Banco** (`deploy/postgres/`): stack independente, volume `pgdata`, sem porta publicada, acessivel pela rede externa `db-net`. **Nao** e tocado pelo pipeline.
- Docker ignora o ufw, por isso nenhuma porta e publicada em `0.0.0.0`.

## Preparacao unica no servidor

```bash
docker network create db-net

# Postgres (copie a pasta deploy/postgres para ex.: ~/infra/postgres)
cd ~/infra/postgres
cp .env.example .env      # edite POSTGRES_PASSWORD
docker compose up -d
```

## Secrets (Settings > Secrets and variables > Actions)

| Secret | Exemplo | Uso |
|---|---|---|
| `DB_CONNECTION_STRING` | `Host=postgres;Port=5432;Database=cachefinancas;Username=cachefinancas;Password=...` | vira `ConnectionStrings__Default` no `deploy/.env` |

Evite espacos e `#` na senha (o `env_file` do compose trataria como comentario/aspas).

Os secrets `BOOTSTRAP_ADMIN_EMAIL` e `BOOTSTRAP_ADMIN_SENHA` do sistema antigo nao sao mais usados e podem ser removidos.

## Expor via Tailscale

```bash
sudo tailscale serve --bg --https=443 http://127.0.0.1:8080
tailscale serve status
```

Acesso: `https://vgdsilva-server.tail223fe1.ts.net`. Para remover: `sudo tailscale serve --https=443 off`.

## Acesso publico via Funnel (opcional)

Por padrao o `tailscale serve` e **tailnet-only**: so abre em dispositivos com o Tailscale instalado. Para abrir de qualquer rede sem cliente, use o Funnel.

> **Aviso:** com o Funnel a URL fica publica na internet. So habilite com autenticacao implementada no app.

1. Painel Tailscale > **Access controls** (JSON editor): garanta o atributo `funnel`:
   ```json
   "nodeAttrs": [
     { "target": ["autogroup:member"], "attr": ["funnel"] }
   ]
   ```
2. Painel > **DNS** > **HTTPS Certificates** > Enable HTTPS (se ainda nao estiver).
3. No servidor:
   ```bash
   sudo tailscale funnel --bg --https=443 http://127.0.0.1:8080
   tailscale funnel status      # deve mostrar (Funnel on)
   ```
4. Para voltar a ficar privado:
   ```bash
   sudo tailscale funnel --https=443 off
   ```

## Logs

```bash
cd <_work>/cache-financas/cache-financas   # checkout do runner
docker compose -f deploy/docker-compose.yml logs -f server web
docker logs -f cache-financas-server       # API
docker logs -f cache-financas-web          # front

journalctl -u 'actions.runner.*' -f        # servico do runner
```

## Rollback

Opcao 1 (recomendada): reverter o commit e dar push.

```bash
git revert <sha> && git push
```

Opcao 2: re-executar em Actions o workflow de um commit anterior (Re-run jobs).

Opcao 3 (emergencia, no servidor): a imagem anterior ainda pode existir sem tag.
`docker images cache-financas-web` e, se houver, `docker tag <id> cache-financas-web:latest && docker compose -f deploy/docker-compose.yml up -d --no-build`.

## Backup dos volumes

```bash
# Banco (logico, preferivel)
docker exec postgres pg_dump -U cachefinancas -Fc cachefinancas > cachefinancas_$(date +%F).dump
# Restore
docker exec -i postgres pg_restore -U cachefinancas -d cachefinancas --clean < cachefinancas_YYYY-MM-DD.dump

# Qualquer volume nomeado (tar)
docker run --rm -v postgres_pgdata:/data -v "$PWD":/backup alpine \
  tar czf /backup/pgdata_$(date +%F).tgz -C /data .
```

Para o tar do `pgdata`, prefira parar o container antes (`docker compose stop`) ou use o `pg_dump`.

## Problemas conhecidos

### Deploy falha: `Bind for 0.0.0.0:8080 failed: port is already allocated`
Outro container ocupa a porta 8080 do host.
```bash
docker ps --format 'table {{.Names}}\t{{.Ports}}' | grep 8080
sudo ss -ltnp | grep ':8080'
```
Remova o container (`docker rm -f <nome>`) ou troque a porta em `deploy/docker-compose.yml`, no `curl` do workflow e neste arquivo. Depois: Actions > **Re-run failed jobs**. Atencao: containers publicados em `0.0.0.0` ficam expostos, pois o Docker ignora o ufw.

### Deploy falha no passo "Garantir rede do banco"
A rede externa nao existe: `docker network create db-net` e suba o Postgres (`deploy/postgres`).

### Site da timeout e `Test-NetConnection` sai pela interface Wi-Fi
Com `tailscale serve` (sem Funnel) o acesso e so para dispositivos conectados a tailnet. O Tailscale do PC/celular precisa estar **Connected**. Para acesso sem cliente, use o Funnel.

### Funnel: `SSL/TLS connection failed` / "nao pode estabelecer conexao segura"
Sintoma: de fora da tailnet (4G) o handshake TLS falha, mas no servidor `curl https://vgdsilva-server.tail223fe1.ts.net/api/health` responde 200 e o container esta `healthy`. O DNS aponta para IPs publicos do Funnel (correto).

Causa provavel: o registro do Funnel ficou desatualizado depois que a rede do host mudou (recriar o container no deploy remove/cria interfaces veth, e o `tailscaled` recalcula endpoints). Nao ha problema na app.

Correcao, no servidor:
```bash
sudo tailscale funnel reset
sudo systemctl restart tailscaled
sleep 10
sudo tailscale funnel --bg --https=443 http://127.0.0.1:8080
tailscale funnel status
```
Aguarde 1-2 min e teste pelo celular em 4G. Se ainda falhar, force o certificado e teste outra porta:
```bash
sudo tailscale cert vgdsilva-server.tail223fe1.ts.net
sudo tailscale funnel --bg --https=8443 http://127.0.0.1:8080
```

Diagnostico rapido:
```bash
tailscale status && tailscale netcheck
sudo journalctl -u tailscaled --since "3 hours ago" --no-pager | tail -40
```

### Checklist quando "o site parou"
1. Actions: o ultimo Deploy ficou verde?
2. `docker ps` mostra `cache-financas-web` e `cache-financas-server` **healthy**?
3. `curl http://127.0.0.1:8080/api/health` responde `{"status":"ok"}`?
4. `tailscale funnel status` mostra `(Funnel on)`?
5. `curl https://vgdsilva-server.tail223fe1.ts.net/api/health` no proprio servidor funciona?
6. Se 1-5 estao ok e de fora falha: reset do Funnel (acima).

### Observacoes
- O `ufw` do servidor esta **inativo**; a protecao atual e nao publicar portas em `0.0.0.0`.
- Um push na `main` (inclusive so de documentacao) dispara o deploy e recria o container.
