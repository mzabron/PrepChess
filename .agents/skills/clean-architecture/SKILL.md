---
name: clean-architecture
description: Skill for maintaining Clean Architecture patterns in PrepChess .NET backend, including CQRS with MediatR, repository patterns, and dependency injection setup.
---

# Clean Architecture Skill

## Project Structure

```
backend/src/
├── PrepChess.Domain/           # Core business logic (NO dependencies except Gera.Chess)
│   ├── Common/                 # BaseEntity, AggregateRoot, IDomainEvent, Result<T>
│   ├── Entities/               # Match, Game, Move, User, OpeningCard, CardProgression, Deck, DeckEntry
│   ├── ValueObjects/           # Fen, Glicko2Rating, TimeControl, MatchScore
│   ├── Enums/                  # GameMode, GameStatus, GameResult, MatchPhase, PieceColor, CardTag
│   ├── Events/                 # Domain events (MoveMade, GameEnded, MatchCompleted, CardBanned, CardDrafted, CardUnlocked)
│   └── Services/               # GameEngine (Gera.Chess wrapper), Glicko2Calculator, CardProgressionService
│
├── PrepChess.Application/      # Use cases (depends on Domain only)
│   ├── Common/
│   │   ├── Interfaces/         # IMatchRepository, IGameRepository, IOpeningCardRepository,
│   │   │                       # IUserRepository, ICardProgressionRepository, IDeckRepository,
│   │   │                       # IApplicationDbContext, ICurrentUserService, ITokenService,
│   │   │                       # IMatchmakingService
│   │   └── Behaviors/          # ValidationBehavior, LoggingBehavior
│   ├── Auth/
│   │   └── Commands/           # Register, Login, ExternalLogin (Google/Apple)
│   ├── Games/
│   │   ├── Commands/           # MakeMove, Resign, OfferDraw, AcceptDraw
│   │   └── Queries/            # GetGame
│   ├── Matches/
│   │   ├── Commands/           # BanCard, DraftCard
│   │   └── Queries/            # GetMatch, GetMatchHistory
│   ├── Matchmaking/
│   │   └── Commands/           # JoinQueue, LeaveQueue
│   ├── OpeningCards/
│   │   └── Queries/            # GetOpeningCards
│   ├── CardProgression/
│   │   └── Queries/            # GetCardTree, GetUnlockedCards, GetCardProgression
│   ├── Decks/
│   │   ├── Commands/           # CreateDeck, UpdateDeck, DeleteDeck
│   │   └── Queries/            # GetDecks
│   ├── Users/
│   │   └── Queries/            # GetProfile, GetLeaderboard
│   └── DependencyInjection.cs  # AddApplication() extension method
│
├── PrepChess.Infrastructure/   # External concerns (depends on Application)
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/     # EF Core entity type configurations (all entities)
│   │   ├── Repositories/       # Concrete repository implementations
│   │   ├── Migrations/         # EF Core migrations
│   │   └── Seed/               # OpeningCardSeeder (Tier 1 starter openings)
│   ├── Identity/               # TokenService, CurrentUserService, GoogleAuthService, AppleAuthService
│   ├── SignalR/                # Notification handlers (MoveMade, GameEnded, MatchCompleted, CardBanned, CardDrafted, CardUnlocked)
│   ├── Matchmaking/            # InMemoryMatchmakingService
│   └── DependencyInjection.cs  # AddInfrastructure() extension method
│
└── PrepChess.Api/              # Entry point (depends on Infrastructure)
    ├── Hubs/                   # GameHub, MatchmakingHub, IGameClient
    ├── Controllers/            # Auth, Users, OpeningCards, Matches, Decks, CardProgression
    ├── Middleware/              # ExceptionHandlingMiddleware (RFC 7807)
    └── Program.cs              # DI composition root

backend/tests/
├── PrepChess.Domain.Tests/         # Unit tests: entities, value objects, services
├── PrepChess.Application.Tests/    # Unit tests: handlers (mocked repos)
└── PrepChess.Infrastructure.Tests/ # Integration tests (Testcontainers + PostgreSQL)
```

## CQRS Command Pattern

### Command (no return value for writes, or Result<T>)
```csharp
public sealed record MakeMoveCommand(
    Guid GameId,
    string PlayerId,
    string From,
    string To,
    string? Promotion = null
) : IRequest<Result<MoveResult>>;
```

### Handler
```csharp
public sealed class MakeMoveCommandHandler(
    IGameRepository gameRepository,
    IGameEngine gameEngine
) : IRequestHandler<MakeMoveCommand, Result<MoveResult>>
{
    public async Task<Result<MoveResult>> Handle(
        MakeMoveCommand request, CancellationToken ct)
    {
        var game = await gameRepository.GetByIdAsync(request.GameId, ct);
        if (game is null) return Result.Failure<MoveResult>("Game not found");

        // --- Authorization guards (run before any mutation) ---

        // 1. Membership: player must be a participant
        var isWhite = game.WhitePlayerId == request.PlayerId;
        var isBlack = game.BlackPlayerId == request.PlayerId;
        if (!isWhite && !isBlack)
            return Result.Failure<MoveResult>("Player is not a participant in this game");

        // 2. Active game: only in-progress games accept moves
        if (game.Status != GameStatus.InProgress)
            return Result.Failure<MoveResult>("Game is not active");

        // 3. Color assignment: verify the player has the color they claim
        var playerColor = isWhite ? PieceColor.White : PieceColor.Black;

        // 4. Turn: the FEN active-color field determines whose turn it is
        var activeColor = game.CurrentFen.ActiveColor; // 'w' or 'b' from FEN
        var isPlayerTurn = (activeColor == 'w' && playerColor == PieceColor.White)
                        || (activeColor == 'b' && playerColor == PieceColor.Black);
        if (!isPlayerTurn)
            return Result.Failure<MoveResult>("It is not your turn");

        // --- Move validation & state mutation ---

        var moveResult = gameEngine.ValidateAndApply(
            game.CurrentFen, request.From, request.To, request.Promotion);
        
        if (!moveResult.IsValid)
            return Result.Failure<MoveResult>(moveResult.Error);

        game.ApplyMove(moveResult); // updates CurrentFen, adds to Moves
        game.AddDomainEvent(new MoveMadeDomainEvent(...));
        
        await gameRepository.UpdateAsync(game, ct);
        
        return Result.Success(moveResult);
    }
}
```

### Validator
```csharp
public sealed class MakeMoveCommandValidator : AbstractValidator<MakeMoveCommand>
{
    public MakeMoveCommandValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.From).NotEmpty().Length(2);
        RuleFor(x => x.To).NotEmpty().Length(2);
    }
}
```

## DI Composition Pattern

Each layer exposes a single `Add{Layer}()` extension method:

```csharp
// In Program.cs:
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

## Domain Events Flow
1. Entity raises event: `game.AddDomainEvent(new MoveMadeDomainEvent(...))`
2. `SaveChangesAsync` override in DbContext dispatches events via MediatR
3. `INotificationHandler<MoveMadeDomainEvent>` in Infrastructure pushes to SignalR
