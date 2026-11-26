# Quickstart / Validação: Base do Projeto

Roteiro para provar, de ponta a ponta, que a feature funciona. Contratos:
[openapi.yaml](./contracts/openapi.yaml), [environment.md](./contracts/environment.md),
[ui-routes.md](./contracts/ui-routes.md).

## Pré-requisitos

- Docker 24+ com Compose v2.
- Para desenvolvimento: .NET SDK 10, Node.js 24.

## 1. Subir a instância (US1)

```bash
cp .env.example .env
# preencha POSTGRES_PASSWORD, QUERYAI_ADMIN_EMAIL e QUERYAI_ADMIN_PASSWORD (>= 12 caracteres)
docker compose up -d
docker compose ps                         # db, api e web "healthy" em até 3 min (SC-002)
curl -s localhost:8080/api/version        # {"version":"..."}
curl -s localhost:8080/health/ready      # status Healthy, check do banco OK
```

**Falha esperada**: deixe `QUERYAI_ADMIN_PASSWORD` vazio → `docker compose up` recusa com
mensagem citando a variável. Com o valor `change-me`, o container `api` encerra e loga o nome da
variável inválida.

**Persistência**: `docker compose down && docker compose up -d` → os usuários continuam existindo.

**Banco fora do ar**: `docker compose stop db` → `/health/ready` retorna 503 indicando o check
`database`.

## 2. Primeiro acesso (US2)

Para avaliação local sem HTTPS, defina `QUERYAI_ALLOW_INSECURE_HTTP=true` no `.env` (sem isso, o
login por `http://localhost:8080` mostra o aviso de HTTPS obrigatório, o que também deve ser
validado). Em produção, acesse via proxy HTTPS (exemplos em `deploy/examples/`).

1. Abra `/` → redireciona para `/login`.
2. Entre com o admin do `.env` → é levado a `/trocar-senha`; tentar abrir `/` volta para lá.
3. Troque a senha → o layout aparece com Início, Conexões, Chat, Histórico e Configurações, todas
   com estado vazio.
4. Saia → `/` exige login novamente.
5. Erre a senha 5 vezes → a mensagem informa o bloqueio temporário (423).

## 3. Usuários e papéis (US3)

1. Em Configurações → Usuários, crie um analista com senha temporária.
2. Em outra janela anônima, entre como o analista → troca de senha obrigatória → o menu não exibe
   Configurações; `/configuracoes` mostra acesso negado; `GET /api/users` → 403.
3. Desative o analista → a próxima ação dele redireciona ao login.
4. Tente desativar o único admin → erro `last_admin`.

## 4. Seed idempotente

- Reinicie `api` → nenhum admin duplicado.
- Troque `QUERYAI_ADMIN_EMAIL` e reinicie → um novo admin é criado; o anterior não muda.
- `docker compose up -d --scale api=2` → continua existindo um único admin por e-mail.

## 5. Testes automatizados

```bash
dotnet test QueryAI.slnx            # unit + arquitetura + integração (Testcontainers; requer Docker)
cd web && npm ci && npm test && npm run lint
```

Esperado: todos verdes, incluindo os testes de autorização (SC-004) e de varredura de segredos
nos logs (SC-007).

## 6. Documentação da API

Em Development (ou com `QUERYAI_ENABLE_API_DOCS=true`): `/openapi/v1.json` e a UI em `/scalar`.
