---
name: chess-domain
description: Skill for working with chess game logic, FEN strings, move validation, and the Gera.Chess library integration in PrepChess Domain and Application layers.
---

# Chess Domain Skill

## Gera.Chess Library Usage

The backend uses **Gera.Chess** (NuGet: `Gera.Chess`, namespace: `Chess`) for all chess logic.

### Key API

```csharp
using Chess;

// Initialize from FEN
var board = new ChessBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

// Make a move (SAN notation)
board.Move("e4");

// Get FEN
string fen = board.ToFen();

// Check game state
bool isEnd = board.IsEndGame;

// ASCII representation (debugging)
string ascii = board.ToAscii();
```

### Important Events
- `OnInvalidMoveKingChecked` — illegal move attempted while in check
- `OnPromotePawn` — pawn promotion required
- `OnEndGame` — game concluded (checkmate, stalemate, draw)

## Architecture Pattern

```
Client (chess.js preview) 
  → SignalR Hub (thin) 
    → MediatR Command (MakeMoveCommand) 
      → GameEngine (Gera.Chess wrapper) 
        → Domain Event (MoveMadeDomainEvent)
          → Notification Handler → IHubContext → Opponent
```

### Server-Authoritative Rule
- The server ALWAYS re-validates every move using Gera.Chess
- Client-side chess.js is ONLY for UX (highlighting legal moves, preventing obviously illegal drags)
- The server's FEN is the source of truth, never the client's

## FEN Value Object
Always wrap raw FEN strings in the `Fen` value object for type safety:
```csharp
public sealed record Fen
{
    public string Value { get; }
    
    public Fen(string value)
    {
        // Validate FEN format
        var board = new ChessBoard(value); // throws if invalid
        Value = value;
    }
}
```

## Opening Cards and FEN
Each `OpeningCard` stores a FEN string representing the board position after a specific opening sequence. When a game starts with an Opening Card, the `ChessBoard` is initialized with that card's FEN instead of the standard starting position.

## Time Controls
- Currently supported: `3+0` (blitz), `10+0` (rapid)
- Timer logic should be in Domain layer
- Server tracks time per player; client displays countdown synced via SignalR
