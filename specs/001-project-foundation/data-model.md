# Data Model: Base do Projeto (001-project-foundation)

Banco interno: PostgreSQL, nomes em snake_case. Todas as datas em UTC (`timestamptz`).

## User (`users`)

| Campo | Tipo | Regras |
|---|---|---|
| `id` | uuid (v7) | PK |
| `name` | varchar(120) | obrigatório, 2–120 caracteres após trim |
| `email` | varchar(254) | obrigatório, formato de e-mail válido, armazenado como informado (trim) |
| `normalized_email` | varchar(254) | `UPPER(trim(email))`, **índice único** |
| `password_hash` | text | hash PBKDF2 (nunca a senha) |
| `role` | varchar(20) | `Admin` \| `Analyst` |
| `is_active` | boolean | padrão `true` |
| `must_change_password` | boolean | `true` ao criar via seed/admin e após redefinição |
| `failed_login_count` | int | ≥ 0, zerado ao logar com sucesso |
| `lockout_end_utc` | timestamptz null | preenchido ao atingir 5 falhas |
| `security_stamp` | uuid | regenerado ao trocar senha, papel ou ao desativar; invalida sessões |
| `created_at_utc` | timestamptz | obrigatório |
| `updated_at_utc` | timestamptz | obrigatório |
| `last_login_at_utc` | timestamptz null | |
| `xmin` | xid | token de concorrência otimista |

**Validações de domínio**

- Senha: mínimo de 12 e máximo de 128 caracteres (FR-012), diferente da senha atual na troca.
- E-mail único de forma case-insensitive.
- Pelo menos um usuário com `role = Admin AND is_active = true` deve existir sempre (FR-019).

**Comportamentos (métodos do agregado)**

- `RegisterFailedLogin(now)`: incrementa o contador; ao chegar a 5, define
  `lockout_end_utc = now + 15 min` e zera o contador.
- `RegisterSuccessfulLogin(now)`: zera o contador e o lockout e atualiza `last_login_at_utc`.
- `IsLockedOut(now)`: `lockout_end_utc > now`.
- `ChangePassword(newHash)`: `must_change_password = false` e novo `security_stamp`.
- `ResetPassword(tempHash)`: `must_change_password = true`, novo `security_stamp` e desbloqueio.
- `ChangeRole(role)`, `Deactivate()`: novo `security_stamp`.
- `Activate()`.

**Estados**

```text
            criar (seed/admin)
                  │
                  ▼
   ┌──── Ativo (must_change_password=true) ──trocar senha──► Ativo
   │              │                                           │
   │        5 falhas de login                           5 falhas de login
   │              ▼                                           ▼
   │       Bloqueado (15 min) ──expira / reset pelo admin──► Ativo
   │
   └── desativar (admin; não pode ser o último admin) ──► Desativado ──reativar──► Ativo
```

## Role (enum)

- `Admin`: acesso total, incluindo Configurações e gestão de usuários.
- `Analyst`: acesso às áreas de uso; sem funcionalidades administrativas.

Conjunto fixo nesta etapa, persistido como string.

## SecurityAuditEvent (`security_audit_events`)

Somente inserção: nenhuma operação de atualização ou exclusão é exposta.

| Campo | Tipo | Regras |
|---|---|---|
| `id` | uuid (v7) | PK |
| `event_type` | varchar(50) | ver lista abaixo |
| `actor_user_id` | uuid null | quem executou (null para login falho de e-mail inexistente e para eventos do sistema) |
| `target_user_id` | uuid null | usuário afetado |
| `occurred_at_utc` | timestamptz | índice |
| `ip_address` | varchar(45) null | IP do cliente (após forwarded headers) |
| `user_agent` | varchar(512) null | truncado |
| `details` | jsonb null | dados não sensíveis (ex.: papel anterior/novo); **nunca** senhas |

**Tipos de evento**: `LoginSucceeded`, `LoginFailed`, `AccountLockedOut`, `Logout`,
`PasswordChanged`, `PasswordReset`, `UserCreated`, `UserUpdated`, `UserRoleChanged`,
`UserDeactivated`, `UserActivated`, `InitialAdminCreated`.

Relacionamentos: `actor_user_id` e `target_user_id` → `users.id` (FK sem cascade).

## Configuração da Instância (não persistida)

Vem das variáveis de ambiente; o contrato está em [contracts/environment.md](./contracts/environment.md).
