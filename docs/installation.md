# Guia de instalação — QueryAI (self-hosted)

Este guia é para o **operador**: a pessoa responsável por instalar e manter a instância do
QueryAI na infraestrutura da organização. Cada instalação atende **uma única organização**.

## 1. Pré-requisitos

- Servidor Linux (amd64) com **Docker 24+** e **Docker Compose v2**.
- 2 vCPU, 2 GB de RAM e 10 GB de disco livres (mínimo para esta versão).
- Um **proxy reverso com HTTPS** (Caddy, nginx, Traefik, load balancer corporativo etc.).
  A instância **não** gerencia certificados; veja a seção 5.

## 2. Configurar

```bash
cp .env.example .env
```

Edite o `.env` e preencha **obrigatoriamente**:

| Variável | O que colocar |
|---|---|
| `POSTGRES_PASSWORD` | Senha forte e aleatória do banco interno (`openssl rand -base64 32`) |
| `QUERYAI_ADMIN_EMAIL` | E-mail do primeiro administrador |
| `QUERYAI_ADMIN_PASSWORD` | Senha inicial do administrador (mínimo de 12 caracteres) |

Valores vazios ou iguais a `change-me` impedem a inicialização, e a mensagem informa qual variável
está faltando. A lista completa de variáveis está em
[`specs/001-project-foundation/contracts/environment.md`](../specs/001-project-foundation/contracts/environment.md).

> O `.env` contém segredos: restrinja as permissões (`chmod 600 .env`) e não o versione.

## 3. Subir a instância

```bash
docker compose up -d
docker compose ps        # db, api e web devem ficar "healthy" (até ~3 minutos)
```

Verifique:

```bash
curl -s http://localhost:8080/health/ready   # {"status":"Healthy","checks":[{"name":"database",...}]}
curl -s http://localhost:8080/api/version    # {"version":"..."}
```

Na primeira inicialização, a API:

1. espera o banco ficar disponível (até 60 s);
2. cria/atualiza a estrutura do banco automaticamente;
3. cria o administrador do `.env`, **caso ainda não exista usuário com esse e-mail**.

## 4. Primeiro acesso

Acesse o endereço HTTPS configurado no seu proxy, entre com o e-mail e a senha do `.env` e
defina uma nova senha (obrigatório no primeiro acesso). Depois, cadastre os demais usuários em
**Configurações → Usuários**.

- Alterar `QUERYAI_ADMIN_PASSWORD` depois **não** muda a senha de uma conta existente.
- Alterar `QUERYAI_ADMIN_EMAIL` e reiniciar **cria um novo administrador**. Isso também serve para
  recuperar o acesso se todos os administradores perderem suas senhas.

### Avaliação local sem HTTPS

Para testar na própria máquina, sem proxy, defina `QUERYAI_ALLOW_INSECURE_HTTP=true` e acesse
`http://localhost:8080`. **Nunca use isso em produção**: a API registra um aviso a cada
inicialização enquanto a opção estiver ativa.

## 5. HTTPS (obrigatório em produção)

O cookie de sessão só é enviado por HTTPS. Coloque um proxy com certificado na frente da porta
`QUERYAI_HTTP_PORT` (padrão 8080), enviando o header `X-Forwarded-Proto: https`:

- [Caddy](../deploy/examples/Caddyfile) (certificado automático)
- [nginx](../deploy/examples/nginx-https.conf) (certificado da organização)

Garanta que a porta 8080 só seja acessível pelo proxy (firewall). Se o proxy roda fora do
servidor do Docker e você quer o IP real dos usuários nos registros de auditoria, inclua o IP do
proxy em `QUERYAI_TRUSTED_PROXIES`.

## 6. Onde ficam os dados e como fazer backup

Todos os dados da instância (usuários, auditoria e, nas próximas versões, conexões e histórico)
ficam no banco interno, no volume Docker **`queryai_queryai-db-data`**. O backup é
responsabilidade do operador.

**Backup** (recomendado: diário, guardado fora do servidor):

```bash
docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > queryai-$(date +%F).dump
```

**Restauração** (em uma instância parada ou nova):

```bash
docker compose up -d db
docker compose exec -T db sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists' < queryai-AAAA-MM-DD.dump
docker compose up -d
```

> `docker compose down` preserva o volume. **`docker compose down -v` apaga todos os dados.**

## 7. Atualizar para uma nova versão

```bash
# 1. faça backup (seção 6)
# 2. ajuste QUERYAI_VERSION no .env para a nova versão
docker compose pull      # ou: docker compose build
docker compose up -d
```

A estrutura do banco é atualizada automaticamente na inicialização, sem perda de dados. Rodar
mais de uma réplica da API ao mesmo tempo é seguro: a atualização é aplicada uma única vez.

## 8. Logs e diagnóstico

```bash
docker compose logs -f api     # logs estruturados em JSON (um evento por linha)
```

- Erros exibidos na interface trazem um **código de rastreamento** (`traceId`), que pode ser
  buscado nos logs.
- Eventos de segurança (logins, bloqueios, alterações de usuários) aparecem nos logs como
  `Security event ...` e ficam gravados na tabela `security_audit_events`.
- Senhas, hashes e segredos nunca são registrados.

| Sintoma | Causa provável |
|---|---|
| `api` reinicia com "A variável de ambiente X é obrigatória" | Variável ausente ou com `change-me` no `.env` |
| "O banco interno não ficou disponível em 60 segundos" | Serviço `db` parado, ou `POSTGRES_*` divergente do volume existente |
| Login mostra "Esta instância exige HTTPS" | Acesso direto por HTTP; use o proxy HTTPS (ou a opção de avaliação local) |
| Conta bloqueada | 5 senhas erradas seguidas: aguarde 15 minutos ou peça a um administrador para redefinir a senha |
