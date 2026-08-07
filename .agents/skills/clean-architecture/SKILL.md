---
name: clean-architecture
description: Skill for maintaining Clean Architecture patterns in PrepChess .NET backend, including CQRS with MediatR, repository patterns, and dependency injection setup.
---

# Clean Architecture Skill

## Project Structure

```text
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
│   ├── Matchmaking/            # InMemoryMatchmakingService
│   └── DependencyInjection.cs  # AddInfrastructure() extension method
│
└── PrepChess.Api/              # Entry point (depends on Infrastructure)
    ├── Hubs/                   # GameHub, MatchmakingHub, IGameClient
    ├── Notifications/          # Domain-event handlers pushing via IHubContext (MoveMade, GameEnded, MatchCompleted, CardBanned, CardDrafted, CardUnlocked)
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
// PlayerId is NOT in the command — the handler resolves the authenticated
// actor via ICurrentUserService to prevent identity spoofing.
public sealed record MakeMoveCommand(
    Guid GameId,
    string From,
    string To,
    string? Promotion = null,
    uint ExpectedVersion = 0  // Optimistic concurrency: version from client's last read
) : IRequest<Result<MoveResult>>;
```

### Handler
```csharp
public sealed class MakeMoveCommandHandler(
    IGameRepository gameRepository,
    IGameEngine gameEngine,
    ICurrentUserService currentUser   // Resolves authenticated identity from the request context
) : IRequestHandler<MakeMoveCommand, Result<MoveResult>>
{
    public async Task<Result<MoveResult>> Handle(
        MakeMoveCommand request, CancellationToken ct)
    {
        // --- Resolve authenticated actor (NEVER trust a client-supplied PlayerId) ---
        var playerId = currentUser.UserId;
        if (playerId is null)
            return Result.Failure<MoveResult>("Unauthenticated");

        var game = await gameRepository.GetByIdAsync(request.GameId, ct);
        if (game is null) return Result.Failure<MoveResult>("Game not found");

        // --- Optimistic concurrency guard ---
        if (request.ExpectedVersion != 0 && request.ExpectedVersion != game.Version)
            return Result.Failure<MoveResult>("Conflict: game state has changed. Reload and retry.");

        // --- Authorization guards (run before any mutation) ---

        // 1. Membership: authenticated user must be a participant
        var isWhite = game.WhitePlayerId == playerId;
        var isBlack = game.BlackPlayerId == playerId;
        if (!isWhite && !isBlack)
            return Result.Failure<MoveResult>("Player is not a participant in this game");

        // 2. Active game: only in-progress games accept moves
        if (game.Status != GameStatus.InProgress)
            return Result.Failure<MoveResult>("Game is not active");

        // 3. Color assignment: derive from authenticated identity
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

        game.ApplyMove(moveResult); // updates CurrentFen, increments Version, adds to Moves
        game.AddDomainEvent(new MoveMadeDomainEvent(...));
        
        // UpdateAsync uses the EF Core concurrency token (Version) to detect
        // conflicting writes. Throws DbUpdateConcurrencyException if another
        // request modified the game between our read and write.
        try
        {
            await gameRepository.UpdateAsync(game, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<MoveResult>("Conflict: game state has changed. Reload and retry.");
        }

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
        // No PlayerId validation — identity comes from ICurrentUserService
        RuleFor(x => x.From).NotEmpty().Length(2);
        RuleFor(x => x.To).NotEmpty().Length(2);
    }
}
```

### Test: Spoofed Identity Cannot Authorize
```csharp
// Proves that a user authenticated as Player A cannot make moves
// in a game where only Player B is a participant, even if a hypothetical
// PlayerId field were somehow injected.
[Fact]
public async Task Handle_AuthenticatedUserNotParticipant_ReturnsFailure()
{
    // Arrange
    var whitePlayerId = Guid.NewGuid().ToString();
    var blackPlayerId = Guid.NewGuid().ToString();
    var attackerId = Guid.NewGuid().ToString(); // not in the game

    var game = CreateTestGame(whitePlayerId, blackPlayerId, GameStatus.InProgress);

    _mockCurrentUser.Setup(x => x.UserId).Returns(attackerId);
    _mockGameRepo.Setup(x => x.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(game);

    var command = new MakeMoveCommand(game.Id, "e2", "e4");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Contain("not a participant");
    _mockGameRepo.Verify(x => x.UpdateAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
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
3. `INotificationHandler<MoveMadeDomainEvent>` in Api (`Api/Notifications/`) pushes to SignalR via `IHubContext`

## Optimistic Concurrency (Game Mutations)

All game-state mutations (moves, resign, draw) must be concurrency-safe. Two concurrent
requests must never silently overwrite each other.

### Strategy: Two-layer protection

1. **Handler layer** — The command carries `ExpectedVersion` (the version the client read).
   The handler compares it against `game.Version` before mutating. Mismatches return a
   `Conflict` result immediately, avoiding unnecessary work.

2. **Persistence layer** — `Game.Version` is configured as an EF Core concurrency token.
   Even if the handler check passes (e.g., `ExpectedVersion` is 0/omitted), the database
   `UPDATE ... WHERE Version = @loaded` will fail with `DbUpdateConcurrencyException`
   if another transaction committed between our read and write.

### Domain Entity
```csharp
// In Game entity (Domain layer)
public uint Version { get; private set; }

public void ApplyMove(MoveResult moveResult)
{
    CurrentFen = moveResult.NewFen;
    Moves.Add(new Move(...));
    Version++;  // Increment on every mutation
}
```

### EF Core Configuration (Infrastructure)
```csharp
public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(g => g.Version)
            .IsConcurrencyToken();  // EF Core adds WHERE Version = @loaded to UPDATE
    }
}
```

### Repository Contract (Application)
```csharp
public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Persists the game. Uses EF Core concurrency token on Version.
    /// Throws DbUpdateConcurrencyException if the stored version differs
    /// from the loaded version (another request modified the game).
    /// </summary>
    Task UpdateAsync(Game game, CancellationToken ct);
}
```
