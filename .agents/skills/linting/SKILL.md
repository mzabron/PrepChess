---
name: linting
description: Skill for linting and code analysis rules in PrepChess — Roslyn analyzers for .NET backend and ESLint/Prettier for the React frontend.
---

# Linting & Code Analysis Skill

## Backend (.NET)

### Roslyn Analyzers

- Enabled solution-wide via `backend/Directory.Build.props`:
  - `EnableNETAnalyzers: true`
  - `AnalysisLevel: latest-recommended`
  - `EnforceCodeStyleInBuild: true`
- **TreatWarningsAsErrors** is ON — any analyzer warning fails the build

### EditorConfig

Location: `backend/.editorconfig`

Key enforced rules:
- **File-scoped namespaces** (error level) — `namespace X;` not `namespace X { }`
- **Naming conventions** — PascalCase for public members, camelCase for private fields with `_` prefix
- **Formatting** — indentation, spacing, braces

### Suppression Policy

- Do NOT suppress analyzer warnings without a justifying comment
- Prefer per-line `#pragma` over global suppression:
  ```csharp
  #pragma warning disable CA1062 // Validated by FluentValidation pipeline behavior
  ```
- `CA2007` (ConfigureAwait): suppress only in `PrepChess.Api` and `PrepChess.Infrastructure` — these are the ASP.NET Core-hosted layers, where the host is known to install no `SynchronizationContext`. Do **not** suppress it in `PrepChess.Domain` or `PrepChess.Application` — those layers must stay framework-agnostic (see Clean Architecture rules), so their caller/hosting context isn't guaranteed.

### Commands

```bash
# Build includes all analyzer checks
cd backend && dotnet build PrepChess.slnx

# Format check (if dotnet format is available)
cd backend && dotnet format PrepChess.slnx --verify-no-changes
```

---

## Frontend (ESLint)

### Configuration

Location: `frontend/eslint.config.js` (flat config format)

Extends:
- `@eslint/js` — recommended JavaScript rules
- `typescript-eslint` — recommended TypeScript rules
- `eslint-plugin-react-hooks` — enforces Rules of Hooks
- `eslint-plugin-react-refresh` — validates HMR-compatible exports
- `eslint-config-prettier` — disables formatting rules that conflict with Prettier

### Key Rule: Separation of Concerns

| Tool      | Handles            |
| :-------- | :----------------- |
| Prettier  | All formatting     |
| ESLint    | Logic errors only  |

ESLint should **never** report formatting issues — `eslint-config-prettier` disables those rules.

### Suppression Policy

- Do NOT add `eslint-disable` comments without a justifying comment:
  ```typescript
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- Third-party lib returns untyped data
  ```

### Commands

```bash
cd frontend && npx eslint .        # Check — must pass with zero errors
cd frontend && npm run lint        # Same via npm script
```

---

## Frontend (Prettier)

### Configuration

Location: `frontend/.prettierrc`

```json
{
  "semi": false,
  "trailingComma": "es5",
  "singleQuote": true,
  "printWidth": 100,
  "tabWidth": 2,
  "useTabs": false,
  "endOfLine": "lf"
}
```

Ignore file: `frontend/.prettierignore` (skips `node_modules`, `dist`, `build`)

### Key Rules
- **No semicolons** — Prettier removes them
- **Single quotes** — `'hello'` not `"hello"`
- **100 char line width** — wider than default 80
- **2-space indent** — no tabs
- **LF line endings** — consistent across OS

### Commands

```bash
cd frontend && npm run format              # Auto-format all files
cd frontend && npx prettier --check .      # Check without modifying (CI mode)
```
