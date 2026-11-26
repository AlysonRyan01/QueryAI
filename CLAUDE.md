# Regras do projeto QueryAI

## Comentários

- **Nunca escreva comentários no código.** Isso vale para todos os arquivos de código e configuração:
  C# (`//`, `///`, `/* */`), JavaScript/JSX (`//`, `/* */`, JSDoc), CSS, Dockerfile, YAML,
  `.env.example`, nginx, `.editorconfig` e similares.
- Use nomes claros de tipos, métodos e variáveis para expressar a intenção no lugar de comentários.
- Explicações, decisões e contexto ficam em `specs/`, `docs/` e no `README.md`, nunca no código.
- Ao editar um arquivo que tenha comentários, remova-os.
- Únicas exceções, por não serem comentários escritos à mão:
  - arquivos gerados por ferramentas (ex.: migrations do EF Core em `Persistence/Migrations/`);
  - diretivas exigidas por ferramentas (ex.: `# syntax=docker/dockerfile:1`).

## Estrutura do código

- Siga `.specify/memory/constitution.md`. Em especial (Princípio III):
  - Casos de uso são serviços da Application chamados diretamente pelos endpoints; nada de
    Command/Handler, mediator, eventos em processo ou decorators de pipeline.
  - Um tipo por arquivo (classe, record, interface, enum ou struct), sem tipos aninhados,
    inclusive nos testes.
  - Dentro de cada contexto, arquivos ficam em subpastas por tipo (`Services`, `Requests`,
    `Validations`, `Dtos`, `Entities`, `Enums`...), nunca soltos na pasta do contexto. A lista de
    nomes permitidos está no Princípio III. Namespaces seguem as pastas.
