---
name: start-branch
description: Skill for creating new feature branches in PrepChess, including naming conventions, branching strategy, and initial setup steps.
---

# Start Branch Skill

## Branching Strategy

All feature work branches from `main`. Never branch from another feature branch unless explicitly coordinating with the user.

## Branch Naming Convention

```
feature/<issue>-{feature-name}     # New features
fix/<issue>-{issue-description}     # Bug fixes
refactor/<issue>-{area}             # Code refactoring
docs/<issue>-{topic}                # Documentation changes
```

The `<issue>` is the GitHub Issue number. Every branch **MUST** include it for traceability across branches → commits → PRs → Project board.

### Rules
- Use **kebab-case** for the name segment: `feature/34-card-progression-tree`
- Keep names short but descriptive (3–5 words max after the issue number)
- If no GitHub Issue exists yet, ask the user to create one or provide the issue number before creating the branch

## Workflow

### 1. Ensure Clean Working Tree
```bash
git status  # Must be clean — no uncommitted changes
```

### 2. Sync with Main
```bash
git checkout main
git pull origin main
```

### 3. Create the Branch
```bash
git checkout -b feature/<issue>-{feature-name}
```

### 4. Verify
```bash
git branch --show-current  # Confirm you're on the new branch
```

## Examples

| Task                        | Issue | Branch Name                            |
| :-------------------------- | :---- | :------------------------------------- |
| Add move validation         | #34   | `feature/34-move-validation`           |
| Fix Glicko-2 rating calc    | #19   | `fix/19-glicko2-rating-calculation`    |
| Refactor game repository    | #45   | `refactor/45-game-repository`          |
| Add opening card seeder     | #52   | `feature/52-opening-card-seeder`       |
| Update SignalR hub patterns | #60   | `refactor/60-signalr-hub-patterns`     |
| Add API documentation       | #8    | `docs/8-api-documentation`             |

## Important Rules

- **NEVER** commit directly to `main`
- **NEVER** force-push to `main`
- Always verify the branch name matches the convention before starting work
- If unsure about the branch type prefix, ask the user
