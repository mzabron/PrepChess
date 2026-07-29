# PrepChess — Agent Rules

## Project Overview
PrepChess is an enterprise-grade, real-time multiplayer chess platform built with:
- **Backend**: .NET 10, ASP.NET Core, SignalR, EF Core, PostgreSQL
- **Frontend**: Vite 8 + React 19 + TypeScript
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → Api)

### Pinned Versions (from scaffolding)
| Technology | Exact Version | Source |
|:-----------|:-------------|:-------|
| .NET SDK | 10.0.101 | `dotnet --version` |
| .NET Runtime | 10.0.1 | `dotnet --list-runtimes` |
| ASP.NET Core | 10.0.1 | `Microsoft.AspNetCore.App` runtime |
| Target Framework | `net10.0` | `Directory.Build.props` |
| Node.js | 22.21.0 | `node --version` |
| npm | 11.8.0 | `npm --version` |
| Vite | 8.0.12 | `package.json` |
| React | 19.2.6 | `package.json` |
| React DOM | 19.2.6 | `package.json` |
| React Router DOM | 7.15.0 | `package.json` |
| TypeScript | 6.0.3 | `package.json` (6.0.2 range) |
| ESLint | 10.3.0 | `package.json` |
| @vitejs/plugin-react | 6.0.1 | `package.json` |

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
| Item | Convention | Example |
|:-----|:-----------|:--------|
| Commands | `{Verb}{Noun}Command` | `MakeMoveCommand` |
| Queries | `Get{Noun}Query` | `GetGameQuery` |
| Handlers | `{Command/Query}Handler` | `MakeMoveCommandHandler` |
| Validators | `{Command}Validator` | `MakeMoveCommandValidator` |
| Domain Events | `{Noun}{Verb}DomainEvent` | `MoveMadeDomainEvent` |
| Repositories | `I{Entity}Repository` | `IGameRepository` |
| Hub Methods (server) | PascalCase verb | `MakeMove`, `JoinGame` |
| Hub Methods (client) | PascalCase verb | `ReceiveMove`, `GameStarted` |
| API Routes | `kebab-case` | `/api/opening-cards` |
| React Components | PascalCase | `GameBoard.tsx` |
| React Hooks | camelCase with `use` | `useSignalR.ts` |

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

## Game Design Constants
- **Time Controls**: 3+0 (blitz) and 10+0 (rapid) — extensible for more later
- **Opening Cards**: Global (same for all players), unlocked via experience progression. No custom player-created cards.
- **Banning Phase**: Each player starts with 5 opening cards, banning is alternating

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
cd frontend && npx eslint .                  # Must pass with zero errors
cd frontend && npm run build                 # TypeScript must compile cleanly
```

### Before committing, verify BOTH:
```bash
# Backend: build + test + no analyzer warnings
cd backend && dotnet build PrepChess.slnx && dotnet test PrepChess.slnx

# Frontend: lint + build
cd frontend && npx eslint . && npm run build
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
- Run `npx eslint .` from `frontend/` to check — must pass with zero errors
- Do NOT add `eslint-disable` comments without a justifying comment explaining why

## Testing
- Unit tests for Domain services and Application handlers
- Integration tests for Infrastructure (database, SignalR)
- Test project naming: `PrepChess.{Layer}.Tests`
- **All tests must pass** before any PR is opened or code is committed

## Project Map
- `/backend`: The .NET 10 solution containing Clean Architecture projects.
  - `src/PrepChess.Domain`: Core business logic, entities, and Gera.Chess integration.
  - `src/PrepChess.Application`: CQRS handlers (MediatR), interfaces, and validation.
  - `src/PrepChess.Infrastructure`: EF Core, PostgreSQL, Redis, SignalR dispatchers.
  - `src/PrepChess.Api`: Controllers, SignalR Hubs, OpenAPI/Scalar, and DI wiring.
- `/frontend`: The Vite + React 19 + TypeScript application.
- `docker-compose.yml`: Local infrastructure (PostgreSQL, Redis).
- `.github/workflows`: CI/CD pipelines.

## Terminal Cheat Sheet
### Run the Infrastructure (Database, Redis)
```bash
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
