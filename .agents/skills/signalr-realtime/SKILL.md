---
name: signalr-realtime
description: Skill for implementing real-time features using SignalR in PrepChess, including hub patterns, client integration, and notification dispatching.
---

# SignalR Real-Time Skill

## Hub Architecture (Thin Hub Pattern)

### Server Hub (`Api/Hubs/GameHub.cs`)
```csharp
public sealed class GameHub : Hub<IGameClient>
{
    private readonly ISender _mediator;

    public GameHub(ISender mediator) => _mediator = mediator;

    public async Task MakeMove(MakeMoveRequest request)
    {
        // PlayerId is intentionally NOT passed here — the handler resolves
        // the authenticated identity via ICurrentUserService to prevent spoofing.
        var command = new MakeMoveCommand(
            GameId: request.GameId,
            From: request.From,
            To: request.To,
            Promotion: request.Promotion,
            ExpectedVersion: request.ExpectedVersion
        );
        
        var result = await _mediator.Send(command, Context.ConnectionAborted);
        
        if (!result.IsSuccess)
            throw new HubException(result.Error);
    }

    public async Task RejoinGame(Guid gameId)
    {
        // Authorize before joining the group — group membership is delivery
        // infrastructure, not an access check. GetGameQuery resolves the caller
        // via ICurrentUserService and fails if they're not a participant/allowed
        // spectator, so an unauthorized caller never gets added to the group.
        var result = await _mediator.Send(
            new GetGameQuery(gameId), Context.ConnectionAborted);

        if (!result.IsSuccess)
            throw new HubException(result.Error);

        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
        await Clients.Caller.GameStateSync(result.Value);
    }
}
```

### Client Interface (`Api/Hubs/IGameClient.cs`)
```csharp
public interface IGameClient
{
    Task ReceiveMove(MoveDto move);
    Task GameStarted(GameStartedDto dto);
    Task GameEnded(GameEndedDto dto);
    Task OpponentJoined(PlayerDto player);
    Task TimerUpdate(TimerDto timer);
    Task GameStateSync(GameStateSyncDto dto);
    Task Error(string message);
}

/// <summary>Full game snapshot sent on reconnect.</summary>
public sealed record GameStateSyncDto(
    string Fen,
    IReadOnlyList<MoveDto> Moves,
    long WhiteTimeRemainingMs,
    long BlackTimeRemainingMs,
    string Status
);
```

### Notification Dispatching (`Api/Notifications/`)

Notification handlers that push to clients live in the **Api layer**, not Infrastructure —
they depend on `GameHub`/`IGameClient`, and Infrastructure must never reference Api.
Register them by including the Api assembly in MediatR's scan in `Program.cs`.

```csharp
// Handles domain events and pushes to clients via IHubContext
public sealed class MoveMadeNotificationHandler : INotificationHandler<MoveMadeDomainEvent>
{
    private readonly IHubContext<GameHub, IGameClient> _hubContext;

    public async Task Handle(MoveMadeDomainEvent notification, CancellationToken ct)
    {
        var group = notification.GameId.ToString();
        await _hubContext.Clients.Group(group)
            .ReceiveMove(new MoveDto { ... });
    }
}
```

## SignalR Groups
- Each active game = one SignalR group (named by `GameId`)
- Players join the group when connecting to a game
- Spectators can also join groups (read-only)

## Frontend Connection (`services/signalrService.ts`)
```typescript
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

const connection = new HubConnectionBuilder()
    .withUrl("https://localhost:5001/hubs/game", {
        accessTokenFactory: () => getAuthToken(),
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();
```

## Reconnection Strategy
- Use `.withAutomaticReconnect()` on the client
- On reconnect, client calls `RejoinGame(gameId)` (defined on `GameHub` above) to re-subscribe to the SignalR group
- Server responds via `IGameClient.GameStateSync(GameStateSyncDto)` with the full current state (FEN, move history, timers, status)

## Future: Redis Backplane
When scaling to multiple servers, add Redis backplane:
```csharp
builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString);
```
