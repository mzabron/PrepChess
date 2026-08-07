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
            Promotion: request.Promotion
        );
        
        var result = await _mediator.Send(command, Context.ConnectionAborted);
        
        if (!result.IsSuccess)
            throw new HubException(result.Error);
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
    Task Error(string message);
}
```

### Notification Dispatching (Infrastructure Layer)
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
- On reconnect, client must call `RejoinGame(gameId)` to re-subscribe to the group
- Server responds with full current game state (FEN + move history + timers)

## Future: Redis Backplane
When scaling to multiple servers, add Redis backplane:
```csharp
builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString);
```
