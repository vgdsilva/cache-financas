# Deploy — Cache Financas

## Arquitetura

```
push na main ─▶ GitHub Actions ─▶ runner self-hosted (vgdsilva-server)
                                        │ docker compose up -d --build
                                        ▼
 Tailscale (HTTPS) ─▶ tailscale serve ─▶ 127.0.0.1:8080 ─▶ container web (:8080)
                                                              │ rede db-net
                                                              ▼
                                                        container postgres (stack separado)
```

- **App** (`deploy/docker-compose.yml`, projeto `cache-financas`): ASP.NET Core 10, porta publicada apenas em `127.0.0.1:8080`. Volume `dataprotection-keys` guarda as chaves de cookies/antiforgery.
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

## Expor via Tailscale

```bash
sudo tailscale serve --bg --https=443 http://127.0.0.1:8080
tailscale serve status
```

Acesso: `https://vgdsilva-server.tail223fe1.ts.net`. Para remover: `sudo tailscale serve --https=443 off`.

## Logs

```bash
cd <_work>/cache-financas/cache-financas   # checkout do runner
docker compose -f deploy/docker-compose.yml logs -f web
docker logs -f cache-financas-web          # equivalente, de qualquer pasta

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
docker run --rm -v cache-financas_dataprotection-keys:/data -v "$PWD":/backup alpine \
  tar czf /backup/keys_$(date +%F).tgz -C /data .
```

Para o tar do `pgdata`, prefira parar o container antes (`docker compose stop`) ou use o `pg_dump`.
