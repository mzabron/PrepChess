---
name: testing
description: Skill for testing conventions, frameworks, and patterns in PrepChess — xUnit/FluentAssertions/NSubstitute for .NET backend and Vitest/React Testing Library for the React frontend.
---

# Testing Skill

## Backend Testing Stack

### Frameworks & Libraries

| Package                        | Version | Layer             | Purpose                                      |
| :----------------------------- | :------ | :---------------- | :------------------------------------------- |
| `xunit`                        | 2.x     | All               | Test framework (facts, theories, fixtures)    |
| `xunit.runner.visualstudio`    | 2.x     | All               | Test discovery and execution in IDE/CI        |
| `Microsoft.NET.Test.Sdk`       | 17.x    | All               | MSBuild integration for `dotnet test`         |
| `FluentAssertions`             | 8.x     | All               | Expressive assertion syntax                   |
| `NSubstitute`                  | 5.x     | Application       | Mocking interfaces (repos, services)          |
| `Testcontainers.PostgreSql`    | 4.x     | Infrastructure    | Spin up real PostgreSQL in Docker for tests   |

### Test Project Mapping

| Test Project                          | Tests For                           | Style         |
| :------------------------------------ | :---------------------------------- | :------------ |
| `PrepChess.Domain.Tests`              | Entities, value objects, services   | Unit          |
| `PrepChess.Application.Tests`         | MediatR handlers, validators       | Unit (mocked) |
| `PrepChess.Infrastructure.Tests`      | EF Core repos, DbContext, SignalR   | Integration   |

### Naming Convention

```
PrepChess.{Layer}.Tests
```

Test classes and methods follow:
```
{ClassUnderTest}Tests.cs
{Method}_{Scenario}_{ExpectedResult}
```

Example:
```csharp
public sealed class MakeMoveCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidMove_ReturnsSuccessWithNewFen() { ... }

    [Fact]
    public async Task Handle_AuthenticatedUserNotParticipant_ReturnsFailure() { ... }

    [Fact]
    public async Task Handle_GameNotActive_ReturnsFailure() { ... }
}
```

---

## Backend Test Patterns

### Arrange-Act-Assert (AAA)

Every test follows the AAA pattern with clear section comments:

```csharp
[Fact]
public async Task Handle_ValidMove_ReturnsSuccessWithNewFen()
{
    // Arrange
    var game = CreateTestGame(GameStatus.InProgress);
    _mockCurrentUser.Setup(x => x.UserId).Returns(game.WhitePlayerId);
    _mockGameRepo.Setup(x => x.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(game);
    _mockGameEngine.Setup(x => x.ValidateAndApply(It.IsAny<Fen>(), "e2", "e4", null))
        .Returns(MoveResult.Valid(new Fen("..."), "e4"));

    var command = new MakeMoveCommand(game.Id, "e2", "e4");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.NewFen.Should().NotBeNull();
    _mockGameRepo.Verify(
        x => x.UpdateAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Mocking with NSubstitute (Application Layer)

Use NSubstitute for all interface mocks in Application handler tests:

```csharp
public sealed class MakeMoveCommandHandlerTests
{
    private readonly IGameRepository _gameRepo = Substitute.For<IGameRepository>();
    private readonly IGameEngine _gameEngine = Substitute.For<IGameEngine>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly MakeMoveCommandHandler _handler;

    public MakeMoveCommandHandlerTests()
    {
        _handler = new MakeMoveCommandHandler(_gameRepo, _gameEngine, _currentUser);
    }
}
```

Key NSubstitute patterns:
```csharp
// Return value
_gameRepo.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);

// Return null
_gameRepo.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).ReturnsNull();

// Verify called
await _gameRepo.Received(1).UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());

// Verify NOT called
await _gameRepo.DidNotReceive().UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
```

### Integration Tests with Testcontainers (Infrastructure Layer)

```csharp
public sealed class GameRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private ApplicationDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingGame_ReturnsGame()
    {
        // Arrange
        var game = new Game(...);
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();
        var repo = new GameRepository(_dbContext);

        // Act
        var result = await repo.GetByIdAsync(game.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(game.Id);
    }
}
```

### FluentAssertions Cheat Sheet

```csharp
// Basics
result.Should().BeTrue();
result.Should().BeFalse();
result.Should().BeNull();
result.Should().NotBeNull();

// Strings
result.Error.Should().Contain("not a participant");
result.Error.Should().Be("exact match");

// Collections
moves.Should().HaveCount(3);
moves.Should().ContainSingle(m => m.San == "e4");
moves.Should().BeEmpty();

// Objects
game.Status.Should().Be(GameStatus.InProgress);
game.Version.Should().Be(1u);

// Exceptions
var act = () => new Fen("invalid");
act.Should().Throw<ArgumentException>();

// Async
var act = async () => await handler.Handle(command, ct);
await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
```

---

## Frontend Testing Stack (Recommended — Install When Needed)

When frontend tests are needed, install:

### Vitest (Test Runner)

Natural companion to Vite — shares the same config, transforms, and plugins.

```bash
cd frontend && npm install -D vitest @vitest/coverage-v8
```

Add to `vite.config.ts`:
```typescript
/// <reference types="vitest/config" />
import { defineConfig } from 'vite'

export default defineConfig({
  // ... existing config
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: true,
  },
})
```

Add script to `package.json`:
```json
{
  "scripts": {
    "test": "vitest run",
    "test:watch": "vitest",
    "test:coverage": "vitest run --coverage"
  }
}
```

### React Testing Library

The React community standard for component testing — tests behavior, not implementation.

```bash
cd frontend && npm install -D @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom
```

Setup file (`src/test/setup.ts`):
```typescript
import '@testing-library/jest-dom/vitest'
```

### Frontend Test Pattern

```typescript
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { GameBoard } from './GameBoard'

describe('GameBoard', () => {
  it('renders the board with initial position', () => {
    render(<GameBoard fen="rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1" />)
    expect(screen.getByRole('grid')).toBeInTheDocument()
  })

  it('highlights legal moves on piece click', async () => {
    const user = userEvent.setup()
    render(<GameBoard fen="..." />)
    await user.click(screen.getByTestId('square-e2'))
    expect(screen.getByTestId('square-e4')).toHaveClass('legal-move')
  })
})
```

### Frontend Test Naming

```
{ComponentName}.test.tsx    # Component tests
{hookName}.test.ts          # Hook tests
{utilName}.test.ts          # Utility tests
```

---

## Commands

```bash
# Backend: run all tests
cd backend && dotnet test PrepChess.slnx

# Backend: run specific test project
cd backend && dotnet test tests/PrepChess.Domain.Tests

# Backend: run with verbose output
cd backend && dotnet test PrepChess.slnx --verbosity normal

# Frontend (after installation):
cd frontend && npm run test           # Run once
cd frontend && npm run test:watch     # Watch mode
cd frontend && npm run test:coverage  # With coverage report
```

## Rules

- **All tests must pass** before any PR is opened or code is committed
- One assertion concept per test (multiple related `Should()` calls are fine)
- Test behavior, not implementation details
- Use descriptive test names that read like specifications
- Domain and Application tests must be fast (no I/O, no database)
- Infrastructure tests use Testcontainers (Docker must be running)
