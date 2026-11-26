# QueryAI

Plataforma self-hosted de análise de dados com IA: o usuário pergunta em português, e o QueryAI
gera um SQL seguro (somente leitura), executa no banco conectado e apresenta os resultados em
tabela, gráfico e explicação.

> **Status**: fundação concluída (instalação, autenticação, usuários e papéis, layout).
> Conexões com bancos, agente de IA e chat chegam nas próximas etapas. Veja `specs/`.

## Arquitetura

```text
 Navegador ──HTTPS──▶ Proxy do cliente ──HTTP──▶ web (nginx + React)
                                                 │  /api, /health
                                                 ▼
                                               api (.NET 10) ──▶ db (PostgreSQL 17)
```

| Pasta | Conteúdo |
|---|---|
| `src/QueryAI.Domain` | Entidades e regras de negócio, sem dependências de framework |
| `src/QueryAI.Application` | Casos de uso (handlers), validação e abstrações (repositórios, hashing, auditoria) |
| `src/QueryAI.Infrastructure` | EF Core/PostgreSQL, migrations, hashing de senha, auditoria, tarefas de inicialização |
| `src/QueryAI.Api` | Minimal APIs, autenticação por cookie, autorização, ProblemDetails, OpenAPI, health checks |
| `web/` | React 19 + Vite em **JavaScript**, React Router e TanStack Query |
| `tests/` | Testes de unidade, arquitetura e integração (Testcontainers) |
| `specs/` | Especificações, planos e contratos (Spec Kit) |
| `.specify/memory/constitution.md` | Princípios do projeto |

## Instalação (operador)

Veja **[docs/installation.md](docs/installation.md)**. Resumo:

```bash
cp .env.example .env   # preencha POSTGRES_PASSWORD, QUERYAI_ADMIN_EMAIL e QUERYAI_ADMIN_PASSWORD
docker compose up -d
```

## Desenvolvimento

Pré-requisitos: .NET SDK 10, Node.js 24 e Docker (para o PostgreSQL e os testes de integração).

```bash
# Banco local
cp docker-compose.override.example.yml docker-compose.override.yml
cp .env.example .env    # preencha os valores obrigatórios
docker compose up -d db

# API em http://localhost:5080 (Development: HTTP permitido, docs em /scalar)
export POSTGRES_HOST=localhost POSTGRES_PASSWORD=... QUERYAI_ADMIN_EMAIL=... QUERYAI_ADMIN_PASSWORD=...
dotnet run --project src/QueryAI.Api

# Web em http://localhost:5173 (proxy de /api para a API)
cd web && npm install && npm run dev
```

### Migrations

```bash
dotnet tool restore
dotnet ef migrations add <Nome> --project src/QueryAI.Infrastructure \
  --startup-project src/QueryAI.Infrastructure -o Persistence/Migrations
```

## Testes

```bash
dotnet test QueryAI.slnx              # unidade + arquitetura + integração (requer Docker)
cd web && npm test && npm run lint
```

Os testes de integração sobem um PostgreSQL com Testcontainers. Para usar um servidor existente,
defina `QUERYAI_TEST_POSTGRES` com uma connection string de superusuário, por exemplo
`Host=localhost;Username=postgres;Password=postgres`.
