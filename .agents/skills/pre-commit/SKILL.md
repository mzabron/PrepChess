---
name: pre-commit
description: Skill for running mandatory pre-commit verification checks before committing code in PrepChess — backend build/test and frontend format/lint/build.
---

# Pre-Commit Skill

## Golden Rule

> **NEVER** commit code that fails any of these checks. If a check fails, fix the issue before proceeding.

## Backend Verification

Run after **every** backend change, before committing:

```bash
cd backend && dotnet build PrepChess.slnx    # Must compile with zero warnings (TreatWarningsAsErrors is on)
cd backend && dotnet test PrepChess.slnx     # All tests must pass
```

### What These Catch
- **Build**: Compilation errors, Roslyn analyzer warnings (promoted to errors), EditorConfig violations
- **Test**: xUnit test failures, FluentAssertions mismatches, integration test regressions

### If Build Fails
1. Read the error output carefully — Roslyn analyzer messages include a rule ID (e.g., `CA1062`)
2. Fix the violation in the source code
3. Only suppress with `#pragma` if there's a justified reason (add a comment explaining why)
4. Re-run the build until zero warnings, zero errors

## Frontend Verification

Run after **every** frontend change, before committing:

```bash
cd frontend && npm run format    # Prettier auto-formats all files
cd frontend && npx eslint .      # Must pass with zero errors
cd frontend && npm run build     # TypeScript must compile cleanly
```

### Order Matters
1. **Format first** — Prettier may change files, which could affect lint results
2. **Lint second** — ESLint catches logic errors that Prettier doesn't handle
3. **Build last** — TypeScript compilation catches type errors

### If Lint Fails
1. Read the ESLint rule name in the output
2. Fix the violation in the source code
3. Only add `eslint-disable` if there's a justified reason (add a comment explaining why)
4. Re-run until zero errors

## Full Pre-Commit Sequence

When changes span **both** backend and frontend, run the full sequence:

```bash
# Backend: build + test
cd backend && dotnet build PrepChess.slnx && dotnet test PrepChess.slnx

# Frontend: format + lint + build
cd frontend && npm run format && npx eslint . && npm run build
```

## Commit Message Convention (Conventional Commits)

Every commit **MUST** follow the [Conventional Commits](https://www.conventionalcommits.org/) format with a scope and the active GitHub Issue number:

```
<type>(<scope>): <description> (#<issue>)
```

### Format Rules

- **`<type>`** — The change category (see table below)
- **`(<scope>)`** — The area of the codebase affected (e.g., `auth`, `game`, `matchmaking`, `ui`, `db`, `signalr`, `cards`, `deck`, `rating`)
- **`<description>`** — Short imperative description of the change
- **`(#<issue>)`** — **MANDATORY**. The GitHub Issue number this commit relates to. This links the commit to the GitHub Project board and provides traceability.

### Commit Types

| Type         | Usage                                       |
| :----------- | :------------------------------------------ |
| `feat`       | New feature or functionality                |
| `fix`        | Bug fix                                     |
| `refactor`   | Code restructuring (no behavior change)     |
| `test`       | Adding or updating tests                    |
| `docs`       | Documentation changes                       |
| `chore`      | Build config, dependencies, tooling         |
| `style`      | Formatting-only changes (should be rare)    |
| `perf`       | Performance improvement                     |
| `ci`         | CI pipeline changes                         |

### Examples

```bash
git commit -m "feat(game): add optimistic concurrency to MakeMove handler (#34)"
git commit -m "fix(auth): prevent identity spoofing in move authorization (#12)"
git commit -m "refactor(signalr): extract hub notification dispatching (#45)"
git commit -m "test(game): add handler tests for stale version rejection (#34)"
git commit -m "chore(db): add EF Core migration for game Version column (#34)"
git commit -m "docs(skills): add pre-commit verification skill (#8)"
```

### Rules
- **Every commit MUST include `(#<issue>)`** at the end — no exceptions. This is how commits are linked to the GitHub Project board.
- Keep the first line under 72 characters
- Use imperative mood: "add feature" not "added feature"
- Keep commits atomic — one logical change per commit
- If a commit touches both backend and frontend, that's fine — but the change should be logically cohesive
- If no GitHub Issue exists yet, ask the user to create one or provide the issue number before committing

