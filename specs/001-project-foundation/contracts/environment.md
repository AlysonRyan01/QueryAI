# Contract: Configuração da Instância (`.env`)

O operador copia `.env.example` para `.env` e preenche os valores. Variáveis **obrigatórias**
vazias ou com valor de exemplo (`change-me`) fazem a inicialização falhar com mensagem
indicando o nome da variável (FR-002).

| Variável | Obrigatória | Padrão | Descrição |
|---|---|---|---|
| `QUERYAI_VERSION` | não | `latest` | Tag das imagens a executar |
| `QUERYAI_HTTP_PORT` | não | `8080` | Porta publicada pelo container `web` (o proxy HTTPS do cliente aponta para ela) |
| `POSTGRES_PASSWORD` | **sim** | — | Senha do usuário do banco interno |
| `POSTGRES_USER` | não | `queryai` | Usuário do banco interno |
| `POSTGRES_DB` | não | `queryai` | Nome do banco interno |
| `QUERYAI_ADMIN_EMAIL` | **sim** | — | E-mail do administrador inicial (criado se não existir) |
| `QUERYAI_ADMIN_PASSWORD` | **sim** | — | Senha inicial (≥ 12 caracteres); a troca é obrigatória no primeiro login |
| `QUERYAI_ADMIN_NAME` | não | `Administrador` | Nome exibido do administrador inicial |
| `QUERYAI_TRUSTED_PROXIES` | não | sub-rede interna do compose | Lista separada por vírgula de IPs/CIDRs cujos headers `X-Forwarded-*` são confiáveis |
| `QUERYAI_SESSION_IDLE_HOURS` | não | `8` | Expiração da sessão por inatividade |
| `QUERYAI_ALLOW_INSECURE_HTTP` | não | `false` | Permite login via HTTP simples (somente para avaliação local; loga um aviso em toda inicialização) |
| `QUERYAI_LOGIN_RATE_LIMIT_PER_MINUTE` | não | `10` | Tentativas de login por minuto por IP |
| `QUERYAI_LOG_LEVEL` | não | `Information` | Nível mínimo de log |
| `QUERYAI_ENABLE_API_DOCS` | não | `false` | Expõe a documentação interativa da API em produção |

Variáveis reservadas para etapas futuras (não usadas nesta feature): `QUERYAI_ENCRYPTION_KEY`,
`QUERYAI_OPENROUTER_API_KEY`, `REDIS_*`.

**Regras**

- Segredos nunca têm valor padrão nas imagens nem no `docker-compose.yml`, que usa
  `${VAR:?mensagem}`.
- O arquivo `.env` está no `.gitignore`; apenas `.env.example` é versionado.
