# Deploy — Cache Financas

## Arquitetura

```
push na main ─▶ GitHub Actions ─▶ runner self-hosted (vgdsilva-server)
                                        │ docker compose up -d --build
                                        ▼
 Tailscale (HTTPS) ─▶ tailscale serve ─▶ 127.0.0.1:8080 ─▶ container web (nginx :8080, Blazor WASM)
                                                              │ /api/* (proxy, rede do compose)
                                                              ▼
                                                        container server (ASP.NET Core :3333)
                                                              │
                                                              ▼
                                                        container postgres (volume pgdata)
```

- **web** (`deploy/Dockerfile.web`, `src/FMS.Web`): Blazor WebAssembly publicado como estaticos e servido pelo nginx (`deploy/nginx.conf`), porta publicada apenas em `127.0.0.1:8080`. O navegador fala so com o web; `/api/*` e repassado ao server com o prefixo removido (`/api/health` -> `server:3333/health`), entao nao ha CORS.
- **Isolamento de falhas:** o `web` nao depende do `server` (sem `depends_on`, healthcheck proprio em `/health`, nginx resolve o server em runtime). Se a API cair, o front continua no ar.
- **server** (`deploy/Dockerfile.server`, `src/FMS.Web.Api`): ASP.NET Core (.NET 11), **sem porta publicada**; so o web o alcanca. Espera o Postgres ficar `healthy` para subir.
- **postgres** (`postgres:17-alpine`): no mesmo compose, volume `pgdata`, sem porta publicada, acessivel apenas pelo server.
- Docker ignora o ufw, por isso nenhuma porta e publicada em `0.0.0.0`.
- A tag da imagem .NET e o ARG `DOTNET_TAG` (padrao `11.0`) nos dois Dockerfiles; ajuste se a imagem preview tiver outra tag.

## Preparacao unica no servidor

Nada alem do runner. O Postgres sobe junto com a stack. (Se existia o Postgres standalone antigo e a rede `db-net`, faca `pg_dump` dele, restaure no novo e depois remova com `docker compose down` na pasta antiga e `docker network rm db-net`.)

## Secrets (Settings > Secrets and variables > Actions)

| Secret | Uso |
|---|---|
| `POSTGRES_PASSWORD` | senha do banco; vira `POSTGRES_PASSWORD` no `deploy/.env` e compoe a connection string do server |

Evite espacos, aspas e `#` na senha. A senha so vale na **primeira** criacao do volume; trocar depois exige `ALTER USER`.

O secret `DB_CONNECTION_STRING` nao e mais usado e pode ser removido.

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
docker logs -f cache-financas-db           # banco

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
docker exec cache-financas-db pg_dump -U cachefinancas -Fc cachefinancas > cachefinancas_$(date +%F).dump
# Restore
docker exec -i cache-financas-db pg_restore -U cachefinancas -d cachefinancas --clean < cachefinancas_YYYY-MM-DD.dump

# Qualquer volume nomeado (tar)
docker run --rm -v cache-financas_pgdata:/data -v "$PWD":/backup alpine \
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
2. `docker ps` mostra `cache-financas-web`, `cache-financas-server` e `cache-financas-db` **healthy**?
3. `curl http://127.0.0.1:8080/api/health` responde `{"status":"ok"}`?
4. `tailscale funnel status` mostra `(Funnel on)`?
5. `curl https://vgdsilva-server.tail223fe1.ts.net/api/health` no proprio servidor funciona?
6. Se 1-5 estao ok e de fora falha: reset do Funnel (acima).

### Observacoes
- O `ufw` do servidor esta **inativo**; a protecao atual e nao publicar portas em `0.0.0.0`.
- Um push na `main` (inclusive so de documentacao) dispara o deploy e recria o container.
