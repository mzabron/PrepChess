# PrepChess — Agent Rules

## Project Overview

PrepChess is an enterprise-grade, real-time multiplayer chess platform built with:

- **Backend**: .NET 10, ASP.NET Core, SignalR, EF Core, PostgreSQL
- **Frontend**: Vite 8 + React 19 + TypeScript
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → Api)

### Pinned Versions

| Technology           | Exact Version | Source                             |
| :------------------- | :------------ | :--------------------------------- |
| .NET SDK             | 10.0.101      | `dotnet --version`                 |
| .NET Runtime         | 10.0.1        | `dotnet --list-runtimes`           |
| ASP.NET Core         | 10.0.1        | `Microsoft.AspNetCore.App` runtime |
| Target Framework     | `net10.0`     | `Directory.Build.props`            |
| Node.js              | 22.21.0       | `node --version`                   |
| npm                  | 11.8.0        | `npm --version`                    |
| Vite                 | 8.0.12        | `package.json`                     |
| React                | 19.2.6        | `package.json`                     |
| React DOM            | 19.2.6        | `package.json`                     |
| React Router DOM     | 7.15.0        | `package.json`                     |
| TypeScript           | 6.0.3         | `package.json` (6.0.2 range)       |
| ESLint               | 10.3.0        | `package.json`                     |
| @vitejs/plugin-react | 6.0.1         | `package.json`                     |

## Architecture Rules

### Clean Architecture Boundaries (STRICT)

- **Domain** (`PrepChess.Domain`): ZERO external dependencies. Only pure C# and `Gera.Chess`. No EF Core, no MediatR, no ASP.NET references.
- **Application** (`PrepChess.Application`): Depends only on Domain. Contains MediatR handlers, FluentValidation, and interface definitions. NEVER references Infrastructure or Api.
- **Infrastructure** (`PrepChess.Infrastructure`): Depends on Application and Domain. Implements interfaces defined in Application (repositories, services). Contains EF Core, Identity, JWT, SignalR dispatchers.
- **Api** (`PrepChess.Api`): Depends on Infrastructure (and transitively Application/Domain). Contains thin controllers, thin SignalR hubs, middleware, and DI wiring.

### Dependency Direction

```
Api → Infrastructure → Application → Domain
```

**NEVER** add a reference going backwards (e.g., Domain referencing Application).

## Code Style & Conventions

### C# (.NET Backend)

- Use **file-scoped namespaces** (`namespace X;` not `namespace X { }`)
- Use **primary constructors** where applicable (.NET 10 feature)
- Use **record types** for DTOs, Commands, and Queries
- Use **sealed** on classes that should not be inherited
- Use **CancellationToken** on all async methods
- All public methods must have XML doc comments
- Repository methods return `Task<T?>` (nullable) for single-entity lookups
- Use `Result<T>` pattern (not exceptions) for expected business failures

### TypeScript (Frontend)

- Use **functional components** only (no class components)
- Use **named exports** (not default exports)
- Use **TypeScript strict mode** (already configured)
- Co-locate component styles (CSS Modules or scoped CSS)
- Prefix custom hooks with `use` (e.g., `useSignalR`, `useGame`)

### Naming Conventions

| Item                 | Convention                | Example                      |
| :------------------- | :------------------------ | :--------------------------- |
| Commands             | `{Verb}{Noun}Command`     | `MakeMoveCommand`            |
| Queries              | `Get{Noun}Query`          | `GetGameQuery`               |
| Handlers             | `{Command/Query}Handler`  | `MakeMoveCommandHandler`     |
| Validators           | `{Command}Validator`      | `MakeMoveCommandValidator`   |
| Domain Events        | `{Noun}{Verb}DomainEvent` | `MoveMadeDomainEvent`        |
| Repositories         | `I{Entity}Repository`     | `IGameRepository`            |
| Hub Methods (server) | PascalCase verb           | `MakeMove`, `JoinGame`       |
| Hub Methods (client) | PascalCase verb           | `ReceiveMove`, `GameStarted` |
| API Routes           | `kebab-case`              | `/api/opening-cards`         |
| React Components     | PascalCase                | `GameBoard.tsx`              |
| React Hooks          | camelCase with `use`      | `useSignalR.ts`              |

## SignalR Rules

- Hubs must be **thin**: only parse input and call `mediator.Send()`
- Use **strongly-typed hubs** (`Hub<IGameClient>`)
- All game state mutations go through MediatR commands — never mutate state in the hub
- Use `IHubContext<GameHub, IGameClient>` in notification handlers to push events

## Chess Logic Rules

- **Server is authoritative**: NEVER trust client-side move validation as final
- Use `Gera.Chess` (`ChessBoard`) for all move validation and game state on the server
- Use `chess.js` on the client ONLY for move previewing / highlighting legal moves
- Always store and transmit positions as FEN strings

## Game Design

### Game Modes

#### Classic Prep (Main Mode)
- Each player builds a **deck of 4 opening cards**, assigning a **color (White/Black) to each card** at deck-building time
- Colors are visible to the opponent during the banning phase
- **Banning phase**: Each player bans 3 of the opponent's 4 cards (alternating bans)
- Result: 1 surviving card per player → **2 games** (deck owner plays their chosen color)

#### Quick Prep
- Each player selects **1 opening card** with a color preference
- **No banning phase**
- **2 games** — one from each card, owners play their chosen color

#### Triple Draft
- **Draft phase**: 3 rounds — each round shows 3 random cards, each player picks 1
- Result: 3 cards per player → **6 games**, alternating colors

### Match Scoring
- 1 point for a win, 0.5 for a draw, 0 for a loss
- Tied total score = **match draw**

### Time Controls
- All modes: **3+0** (blitz) or **10+0** (rapid) — selected when queueing

### Opening Cards
- **Global** (same for all players) — no custom player-created cards
- Entity: Title, FEN, Description, ECO code, Move Sequence, Tags (aggressive, positional, theoretical, etc.), Tier
- **Progression Tree**: Tier 1 cards are free. Higher-tier cards unlock by playing/winning games with their parent opening (e.g., Queen's Gambit → Catalan unlocks after 30 games in QG)
- When building a deck, players assign a **preferred color** to each card

### Spectating
- Other players can spectate live games (read-only, join SignalR group)

### Authentication
- **Email + password** via ASP.NET Core Identity
- **Google OAuth** and **Apple Sign In** as external login providers

### Rating System
- **Glicko-2** with separate ratings per game mode
- Initial: 1500 rating, 350 RD, 0.06 volatility

## Database Rules

- Use **EF Core Code-First Migrations** — never edit the database schema manually
- All entities have `CreatedAt` and `UpdatedAt` timestamps (set in `SaveChangesAsync` override)
- Use `Guid` for all primary keys
- Soft-delete pattern for games (never hard-delete game records)

## Git Workflow

- Branch from `main` for features: `feature/{feature-name}`
- Keep commits atomic and descriptive
- Run `dotnet build` before committing backend changes
- Run `npm run build` before committing frontend changes

## Verification Rules (MANDATORY)

### After EVERY backend change, run:

```bash
cd backend && dotnet build PrepChess.slnx    # Must compile with zero warnings (TreatWarningsAsErrors is on)
cd backend && dotnet test PrepChess.slnx     # All tests must pass
```

### After EVERY frontend change, run:

```bash
cd frontend && npm run format                # Prettier formatting
cd frontend && npx eslint .                  # Must pass with zero errors
cd frontend && npm run build                 # TypeScript must compile cleanly
```

### Before committing, verify BOTH:

```bash
# Backend: build + test + no analyzer warnings
cd backend && dotnet build PrepChess.slnx && dotnet test PrepChess.slnx

# Frontend: format + lint + build
cd frontend && npm run format && npx eslint . && npm run build
```

> **NEVER** commit code that fails any of these checks. If a check fails, fix the issue before proceeding.

## Linting & Code Analysis

### Backend (.NET)

- **Roslyn Analyzers** are enabled solution-wide via `Directory.Build.props` (`AnalysisLevel: latest-recommended`)
- **TreatWarningsAsErrors** is ON — any analyzer warning fails the build
- **EditorConfig** (`.editorconfig`) enforces: file-scoped namespaces (error), naming conventions, formatting
- Do NOT suppress analyzer warnings without a justifying comment explaining why
- If adding a new suppression, prefer per-line `#pragma` over global suppression

### Frontend (TypeScript/React)

- **ESLint** config is in `frontend/eslint.config.js`
- Extends: `@eslint/js` recommended, `typescript-eslint` recommended, `react-hooks`, `react-refresh`
- **eslint-config-prettier** disables formatting rules that conflict with Prettier
- Run `npx eslint .` from `frontend/` to check — must pass with zero errors
- Do NOT add `eslint-disable` comments without a justifying comment explaining why

### Frontend (Prettier)

- **Prettier** config is in `frontend/.prettierrc`
- Enforces: no semicolons, single quotes, 2-space indent, 100 char line width, LF line endings
- Ignore file: `frontend/.prettierignore` (skips `node_modules`, `dist`, `build`)
- Run `npm run format` from `frontend/` to auto-format all files
- Prettier handles **all formatting** — ESLint handles **only logic errors**

## Testing

- Unit tests for Domain services and Application handlers
- Integration tests for Infrastructure (database, SignalR)
- Test project naming: `PrepChess.{Layer}.Tests`
- **All tests must pass** before any PR is opened or code is committed

## Project Map

- `/backend`: The .NET 10 solution (Clean Architecture)
  - `src/PrepChess.Domain`: Core entities, value objects, enums, domain events, services (GameEngine, Glicko2Calculator, CardProgressionService). Only dependency: `Gera.Chess`.
  - `src/PrepChess.Application`: CQRS handlers (MediatR), FluentValidation, interface definitions.
  - `src/PrepChess.Infrastructure`: EF Core (PostgreSQL), ASP.NET Identity, JWT, Google/Apple OAuth, SignalR notification dispatchers, Redis backplane.
  - `src/PrepChess.Api`: Thin controllers, thin SignalR hubs, middleware, OpenAPI/Scalar, DI wiring.
  - `tests/PrepChess.Domain.Tests`: Domain unit tests.
  - `tests/PrepChess.Application.Tests`: Application handler unit tests.
  - `tests/PrepChess.Infrastructure.Tests`: Integration tests (Testcontainers + PostgreSQL).
- `/frontend`: Vite 8 + React 19 + TypeScript application.
- `docker-compose.yml`: Local infrastructure (PostgreSQL, Redis).
- `.env` / `.env.example`: Environment variables for Docker Compose (DB credentials, Redis password). `.env` is git-ignored; `.env.example` is tracked as a template.
- `.github/workflows/ci.yml`: CI pipeline (build + test backend, lint + build frontend).

## Terminal Cheat Sheet

### Run the Infrastructure (Database, Redis)
```bash
# First time: copy .env.example to .env and set your passwords
cp .env.example .env

# Start containers (PostgreSQL on :5432, Redis on :6379)
docker compose up -d
```

### Run the Backend (API & SignalR)
```bash
cd backend/src/PrepChess.Api
dotnet run
```
*Scalar OpenAPI UI will be available at `https://localhost:<port>/scalar/v1`*

### Run the Frontend
```bash
cd frontend
npm run dev
```

### Formatting & Linting (Frontend)
```bash
cd frontend
npm run format  # Runs Prettier
npm run lint    # Runs ESLint
```

### Full Verification
```bash
# Backend
cd backend && dotnet build PrepChess.slnx && dotnet test PrepChess.slnx

# Frontend
cd frontend && npm run format && npx eslint . && npm run build
```
