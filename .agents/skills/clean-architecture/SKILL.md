---
name: clean-architecture
description: Skill for maintaining Clean Architecture patterns in PrepChess .NET backend, including CQRS with MediatR, repository patterns, and dependency injection setup.
---

# Clean Architecture Skill

## Project Structure

```
backend/src/
├── PrepChess.Domain/           # Core business logic (NO dependencies)
│   ├── Common/                 # BaseEntity, AggregateRoot, IDomainEvent
│   ├── Entities/               # Game, User, OpeningCard
│   ├── ValueObjects/           # Fen, Move, Rating
│   ├── Enums/                  # GameStatus, GameResult, TimeControl
│   ├── Events/                 # Domain events (MoveMade, GameEnded)
│   └── Services/               # IGameEngine + GameEngine implementation
│
├── PrepChess.Application/      # Use cases (depends on Domain only)
│   ├── Common/
│   │   ├── Interfaces/         # IGameRepository, IApplicationDbContext, etc.
│   │   └── Behaviors/          # ValidationBehavior, LoggingBehavior
│   ├── Games/
│   │   ├── Commands/           # CreateGame, MakeMove, Resign, etc.
│   │   └── Queries/            # GetGame, GetGameHistory, etc.
│   ├── OpeningCards/
│   │   └── Queries/            # GetOpeningCards, GetCardsByCategory
│   ├── Auth/
│   │   └── Commands/           # Register, Login
│   └── DependencyInjection.cs  # AddApplication() extension method
│
├── PrepChess.Infrastructure/   # External concerns (depends on Application)
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/     # EF Core entity type configurations
│   │   ├── Repositories/       # Concrete repository implementations
│   │   └── Migrations/         # EF Core migrations
│   ├── Identity/               # JWT token service, CurrentUserService
│   ├── SignalR/                # Notification handlers that use IHubContext
│   └── DependencyInjection.cs  # AddInfrastructure() extension method
│
└── PrepChess.Api/              # Entry point (depends on Infrastructure)
    ├── Hubs/                   # Thin SignalR hubs
    ├── Controllers/            # Thin REST controllers
    ├── Middleware/              # Exception handling, request logging
    └── Program.cs              # DI composition root
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
