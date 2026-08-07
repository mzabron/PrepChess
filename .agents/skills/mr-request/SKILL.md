---
name: mr-request
description: Skill for creating merge/pull requests in PrepChess, including title format, description template, checklist, and review guidelines.
---

# Merge Request (PR) Skill

## PR Title Format

PR titles follow the same Conventional Commits format as commit messages, including the scope and GitHub Issue number:

```
<type>(<scope>): <description> (#<issue>)
```

Use the same types and scopes as commit messages (see `pre-commit` skill):

| Type         | Usage                                       |
| :----------- | :------------------------------------------ |
| `feat`       | New feature or functionality                |
| `fix`        | Bug fix                                     |
| `refactor`   | Code restructuring (no behavior change)     |
| `test`       | Adding or updating tests                    |
| `docs`       | Documentation changes                       |
| `chore`      | Build config, dependencies, tooling         |

### Examples
- `feat(game): add MakeMove command handler with optimistic concurrency (#34)`
- `fix(rating): correct Glicko-2 volatility calculation (#19)`
- `refactor(game): extract game authorization into pipeline behavior (#45)`

## PR Description Template

Use this structure for every PR description. The **closing keyword** in the first line is **MANDATORY** for PRs that fully complete a task — it automatically moves the linked GitHub Issue to the "Done" column on the Project board.

```markdown
Closes #<issue>

## What
Brief description of what this PR does (1-2 sentences).

## Why
The motivation or problem this solves.

## How
Key implementation decisions. Highlight non-obvious choices.

## Changes
- List of notable file changes grouped by area
- Backend: ...
- Frontend: ...

## Testing
- What tests were added/updated
- Manual verification steps performed
```

### GitHub Closing Keywords

When a PR **fully completes** the work for a GitHub Issue, its description **MUST** include one of these closing keywords followed by the issue number. This auto-closes the issue and moves it to "Done" on the GitHub Project board when the PR is merged:

| Keyword           | Example         |
| :---------------- | :-------------- |
| `Closes #<issue>` | `Closes #34`    |
| `Fixes #<issue>`  | `Fixes #12`     |

- Place the closing keyword on its **own line at the top** of the PR description (before the `## What` section)
- Use `Fixes` for bug fix PRs, `Closes` for feature/refactor/other PRs
- If a PR addresses **multiple issues**, list each on a separate line:
  ```
  Closes #34
  Closes #35
  ```
- If a PR only **partially** addresses an issue (the issue still has remaining work), do **NOT** use a closing keyword — instead reference it in the description body: `Part of #34`

## Pre-Submit Checklist

Before creating the PR, verify ALL of the following:

### Backend (if changed)
- [ ] `dotnet build PrepChess.slnx` — zero warnings, zero errors
- [ ] `dotnet test PrepChess.slnx` — all tests pass
- [ ] No Roslyn analyzer suppressions without justifying comments
- [ ] New public methods have XML doc comments

### Frontend (if changed)
- [ ] `npm run format` — Prettier applied
- [ ] `npx eslint .` — zero errors
- [ ] `npm run build` — TypeScript compiles cleanly
- [ ] No `eslint-disable` comments without justifying comments

### General
- [ ] Branch is up to date with `main` (rebase or merge)
- [ ] All commit messages follow `<type>(<scope>): <description> (#<issue>)` format
- [ ] Every commit includes the GitHub Issue number `(#N)`
- [ ] No unrelated changes included
- [ ] CI pipeline passes (`.github/workflows/ci.yml`)

## PR Creation Command

```bash
# Using GitHub CLI (gh)
gh pr create --title "feat(game): add move validation (#34)" --body "Closes #34

## What
Add server-side move validation..."

# Or interactively
gh pr create
```

## Review Guidelines

When reviewing PRs (or preparing for review):

- **Architecture**: Verify Clean Architecture boundaries are respected (no backward references)
- **Security**: Authorization uses `ICurrentUserService`, not client-supplied IDs
- **Concurrency**: Game mutations use optimistic versioning (`ExpectedVersion`)
- **Naming**: Follows the naming conventions table in AGENTS.md
- **Tests**: New behavior has corresponding test coverage

## Merge Strategy

- **Squash merge** for feature branches (keeps `main` history clean)
- Delete the source branch after merge
- Never merge a PR with failing CI
