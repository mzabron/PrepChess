---
name: chess-domain
description: Skill for working with chess game logic, FEN strings, move validation, the Gera.Chess library integration, and the Opening Card system in PrepChess Domain and Application layers.
---

# Chess Domain Skill

## Gera.Chess Library Usage

The backend uses **Gera.Chess** (NuGet: `Gera.Chess`, version `1.2.0`, namespace: `Chess`) for all chess logic. The package is referenced by `PrepChess.Domain` only — the API examples below are verified against 1.2.0.

### Key API

```csharp
using Chess;

// Initialize from FEN
var board = ChessBoard.LoadFromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

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

```text
Client (chess.js preview) 
  → SignalR Hub (thin) 
    → MediatR Command (MakeMoveCommand) 
      → GameEngine (Gera.Chess wrapper) 
        → Domain Event (MoveMadeDomainEvent)
          → Notification Handler → IHubContext → Opponent + Spectators
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

    /// <summary>'w' or 'b' — extracted from the FEN active-color field.</summary>
    public char ActiveColor { get; }

    public Fen(string value)
    {
        // Validate FEN format
        var board = ChessBoard.LoadFromFen(value); // throws if invalid
        Value = value;
        ActiveColor = value.Split(' ')[1][0]; // 'w' or 'b'
    }
}
```

## Entity Relationships

```text
Match (Aggregate Root)
├── GameMode (ClassicPrep | QuickPrep | TripleDraft)
├── TimeControl (3+0 or 10+0)
├── Player1 / Player2 (User entities or Guests)
├── Player1DeckSnapshot / Player2DeckSnapshot
├── MatchPhase (WaitingForOpponent → Banning/Picking/Drafting → InProgress → Completed)
├── BannedCards / PickedCards / DraftedCards
├── FinalScore (MatchScore value object)
└── Games[] (one Game per opening card used)
    ├── OpeningCard (determines StartingFen)
    ├── WhitePlayer / BlackPlayer (assigned via deck color preference)
    ├── CurrentFen
    ├── Version (uint, EF Core concurrency token — incremented on every move)
    ├── MoveCount (int, derived from Moves.Count for quick access)
    ├── Moves[]
    ├── GameStatus / GameResult
    └── Clocks (WhiteTimeRemainingMs, BlackTimeRemainingMs)
```

## Opening Cards and FEN

Each `OpeningCard` stores:
- **Title** — e.g., "Queen's Gambit"
- **FEN** — the board position after the opening moves
- **Description** — opening character description
- **Style Profile** — `CardStyleProfile`, three 0–100 axis scores driving a slider/dot UI rather than discrete tags: Tactical↔Positional, Theoretical↔Intuitive, Easy↔Hard
- **ECO Code** — standard classification (e.g., "D06")
- **Move Sequence** — SAN moves leading to the FEN (for display)
- **Tier** — 1 = starter (free), 2+ = unlockable
- **ParentCardId** — nullable, forms progression tree
- **UnlockGamesRequired / UnlockWinsRequired** — conditions to unlock from parent

When a game starts with an Opening Card, the `ChessBoard` is initialized with that card's FEN instead of the standard starting position.

### Card Progression Tree
*Note: Guest players do not participate in progression. They cannot gain experience or unlock new opening cards.*
```text
Queen's Gambit (Tier 1, free)
├── QG Declined (Tier 2) — 20 games in QG
├── QG Accepted (Tier 2) — 15 games in QG
│   └── QGA Central Var (Tier 3) — 10 wins in QGA
└── Catalan Game (Tier 2) — 30 games in QG
```

## Deck Building
- Players build decks before queueing for a match
- Each `DeckEntry` pairs an `OpeningCard` with a `PreferredColor` (White/Black)
- Classic Prep: 4 cards per deck
- Quick Prep: 1 card per deck
- Triple Draft: cards are drafted in-match (no pre-built deck)
- The color choice is locked in at deck-building time and visible to the opponent during banning and picking

## Time Controls
- Currently supported: `3+0` (blitz), `10+0` (rapid)
- Timer logic should be in Domain layer
- Server tracks time per player; client displays countdown synced via SignalR
