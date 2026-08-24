using FluentAssertions;
using PrepChess.Domain.Common;
using PrepChess.Domain.Entities;
using PrepChess.Domain.Events;
using Xunit;

namespace PrepChess.Domain.Tests.Entities;

public sealed class CardProgressionTests
{
    [Fact]
    public void Create_ReturnsProgressionWithZeroCounts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        // Act
        var progression = CardProgression.Create(userId, cardId);

        // Assert
        progression.UserId.Should().Be(userId);
        progression.OpeningCardId.Should().Be(cardId);
        progression.GamesPlayed.Should().Be(0);
        progression.GamesWon.Should().Be(0);
        progression.IsUnlocked.Should().BeFalse();
        progression.UnlockedAt.Should().BeNull();
    }

    [Fact]
    public void RecordGameResult_Win_IncrementsBothCounters()
    {
        // Arrange
        var progression = CardProgression.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = progression.RecordGameResult(isWin: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progression.GamesPlayed.Should().Be(1);
        progression.GamesWon.Should().Be(1);
    }

    [Fact]
    public void RecordGameResult_Loss_IncrementsOnlyGamesPlayed()
    {
        // Arrange
        var progression = CardProgression.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = progression.RecordGameResult(isWin: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progression.GamesPlayed.Should().Be(1);
        progression.GamesWon.Should().Be(0);
    }

    [Fact]
    public void RecordGameResult_MultipleCalls_AccumulatesCorrectly()
    {
        // Arrange
        var progression = CardProgression.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act
        progression.RecordGameResult(isWin: true);
        progression.RecordGameResult(isWin: false);
        progression.RecordGameResult(isWin: true);
        progression.RecordGameResult(isWin: false);

        // Assert
        progression.GamesPlayed.Should().Be(4);
        progression.GamesWon.Should().Be(2);
    }

    [Fact]
    public void RecordGameResult_WhenAlreadyUnlocked_ReturnsFailure()
    {
        // Arrange
        var progression = CardProgression.Create(Guid.NewGuid(), Guid.NewGuid());
        progression.Unlock();

        // Act
        var result = progression.RecordGameResult(isWin: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CardProgression.AlreadyUnlocked);
    }

    [Fact]
    public void Unlock_SetsIsUnlockedAndUnlockedAt()
    {
        // Arrange
        var progression = CardProgression.Create(Guid.NewGuid(), Guid.NewGuid());
        var beforeUnlock = DateTime.UtcNow;

        // Act
        var result = progression.Unlock();

        // Assert
        result.IsSuccess.Should().BeTrue();
        progression.IsUnlocked.Should().BeTrue();
        progression.UnlockedAt.Should().NotBeNull();
        progression.UnlockedAt.Should().BeOnOrAfter(beforeUnlock);
    }

    [Fact]
    public void Unlock_RaisesCardUnlockedDomainEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var progression = CardProgression.Create(userId, cardId);

        // Act
        progression.Unlock();

        // Assert
        progression.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CardUnlockedDomainEvent>()
            .Which.Should().BeEquivalentTo(new CardUnlockedDomainEvent(userId, cardId));
    }

    [Fact]
    public void Unlock_WhenAlreadyUnlocked_ReturnsFailure()
    {
        // Arrange
        var progression = CardProgression.Create(Guid.NewGuid(), Guid.NewGuid());
        progression.Unlock();

        // Act
        var result = progression.Unlock();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CardProgression.AlreadyUnlocked);
    }
}
