using FluentAssertions;
using PrepChess.Domain.Common;
using PrepChess.Domain.Entities;
using PrepChess.Domain.Events;
using PrepChess.Domain.ValueObjects;
using Xunit;

namespace PrepChess.Domain.Tests.Services;

public sealed class CardProgressionServiceTests
{
    private readonly Domain.Services.CardProgressionService _service = new();

    private static readonly Fen ValidFen = Fen.Create("rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR b KQkq d3 0 1").Value;
    private static readonly CardStyleProfile ValidProfile = CardStyleProfile.Create(50, 50, 50).Value;

    private static OpeningCard CreateTier1Card()
    {
        return OpeningCard.Create(
            "Queen's Gambit",
            ValidFen,
            "A solid opening",
            "D06",
            "1.d4 d5 2.c4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0).Value;
    }

    private static OpeningCard CreateTier2Card(int unlockGames = 20, int unlockWins = 5)
    {
        return OpeningCard.Create(
            "QG Declined",
            ValidFen,
            "Classical defense",
            "D30",
            "1.d4 d5 2.c4 e6",
            ValidProfile,
            tier: 2,
            parentCardId: Guid.NewGuid(),
            unlockGamesRequired: unlockGames,
            unlockWinsRequired: unlockWins).Value;
    }

    [Fact]
    public void CheckAndUnlock_GuestUser_ReturnsFailure()
    {
        // Arrange
        var card = CreateTier2Card();
        var progression = CardProgression.Create(Guid.NewGuid(), card.Id);

        // Act
        var result = _service.CheckAndUnlock(progression, card, isGuestUser: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CardProgressionService.GuestCannotProgress);
        progression.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void CheckAndUnlock_AlreadyUnlocked_ReturnsFailure()
    {
        // Arrange
        var card = CreateTier2Card();
        var progression = CardProgression.Create(Guid.NewGuid(), card.Id);
        progression.Unlock();
        progression.ClearDomainEvents();

        // Act
        var result = _service.CheckAndUnlock(progression, card, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CardProgression.AlreadyUnlocked);
    }

    [Fact]
    public void CheckAndUnlock_Tier1Card_ReturnsFailure()
    {
        // Arrange
        var card = CreateTier1Card();
        var progression = CardProgression.Create(Guid.NewGuid(), card.Id);

        // Act
        var result = _service.CheckAndUnlock(progression, card, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CardProgressionService.Tier1AlwaysUnlocked);
    }

    [Fact]
    public void CheckAndUnlock_RequirementsNotMet_GamesShort_ReturnsFailure()
    {
        // Arrange
        var card = CreateTier2Card(unlockGames: 20, unlockWins: 5);
        var progression = CardProgression.Create(Guid.NewGuid(), card.Id);

        // Play 10 games, win all — games short, wins met
        for (int i = 0; i < 10; i++)
        {
            progression.RecordGameResult(isWin: true);
        }

        // Act
        var result = _service.CheckAndUnlock(progression, card, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CardProgressionService.RequirementsNotMet);
        progression.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void CheckAndUnlock_RequirementsNotMet_WinsShort_ReturnsFailure()
    {
        // Arrange
        var card = CreateTier2Card(unlockGames: 20, unlockWins: 5);
        var progression = CardProgression.Create(Guid.NewGuid(), card.Id);

        // Play 20 games, win 3 — games met, wins short
        for (int i = 0; i < 3; i++)
        {
            progression.RecordGameResult(isWin: true);
        }

        for (int i = 0; i < 17; i++)
        {
            progression.RecordGameResult(isWin: false);
        }

        // Act
        var result = _service.CheckAndUnlock(progression, card, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CardProgressionService.RequirementsNotMet);
        progression.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void CheckAndUnlock_RequirementsMet_UnlocksProgression()
    {
        // Arrange
        var card = CreateTier2Card(unlockGames: 20, unlockWins: 5);
        var progression = CardProgression.Create(Guid.NewGuid(), card.Id);

        // Play 25 games, win 8 — both exceeded
        for (int i = 0; i < 8; i++)
        {
            progression.RecordGameResult(isWin: true);
        }

        for (int i = 0; i < 17; i++)
        {
            progression.RecordGameResult(isWin: false);
        }

        // Act
        var result = _service.CheckAndUnlock(progression, card, isGuestUser: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progression.IsUnlocked.Should().BeTrue();
        progression.UnlockedAt.Should().NotBeNull();
        progression.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CardUnlockedDomainEvent>();
    }

    [Fact]
    public void CheckAndUnlock_ExactRequirements_UnlocksProgression()
    {
        // Arrange
        var card = CreateTier2Card(unlockGames: 10, unlockWins: 3);
        var progression = CardProgression.Create(Guid.NewGuid(), card.Id);

        // Exact: 10 games, 3 wins
        for (int i = 0; i < 3; i++)
        {
            progression.RecordGameResult(isWin: true);
        }

        for (int i = 0; i < 7; i++)
        {
            progression.RecordGameResult(isWin: false);
        }

        // Act
        var result = _service.CheckAndUnlock(progression, card, isGuestUser: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progression.IsUnlocked.Should().BeTrue();
    }
}
