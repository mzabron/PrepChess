using FluentAssertions;
using PrepChess.Domain.Entities;
using PrepChess.Domain.Events;
using PrepChess.Domain.Services;
using PrepChess.Domain.ValueObjects;
using Xunit;

namespace PrepChess.Domain.Tests.Services;

public sealed class CardProgressionServiceTests
{
    private readonly CardProgressionService _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Fen _validFen = Fen.Create("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1").Value;
    private readonly CardStyleProfile _validProfile = CardStyleProfile.Create(50, 50, 50).Value;

    private OpeningCard CreateTier1Card() =>
        OpeningCard.Create("Parent Opening", _validFen, "A Tier 1 opening", "A00", "1.e4", _validProfile, 1, null, 0).Value;

    private OpeningCard CreateTier2Card(Guid parentCardId, int gamesRequired) =>
        OpeningCard.Create("Child Opening", _validFen, "A Tier 2 opening", "A00", "1.e4 e5", _validProfile, 2, parentCardId, gamesRequired).Value;

    private CardProgression CreateProgression(Guid openingCardId) =>
        CardProgression.Create(_userId, openingCardId).Value;

    [Fact]
    public void CheckAndUnlock_GuestUser_ReturnsFailure()
    {
        // Arrange
        var parentCard = CreateTier1Card();
        var targetCard = CreateTier2Card(parentCard.Id, 5);
        var parentProgression = CreateProgression(parentCard.Id);
        var targetProgression = CreateProgression(targetCard.Id);

        // Act
        var result = _service.CheckAndUnlock(parentProgression, targetProgression, targetCard, isGuestUser: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgressionService.GuestCannotProgress);
    }

    [Fact]
    public void CheckAndUnlock_Tier1Card_ReturnsSuccess()
    {
        // Arrange
        var tier1Card = CreateTier1Card();
        var parentProgression = CreateProgression(Guid.NewGuid());
        var targetProgression = CreateProgression(tier1Card.Id);

        // Act
        var result = _service.CheckAndUnlock(parentProgression, targetProgression, tier1Card, isGuestUser: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CheckAndUnlock_ParentProgressionMismatch_ReturnsFailure()
    {
        // Arrange
        var parentCard = CreateTier1Card();
        var targetCard = CreateTier2Card(parentCard.Id, 5);
        var wrongParentProgression = CreateProgression(Guid.NewGuid()); // doesn't match parentCard.Id
        var targetProgression = CreateProgression(targetCard.Id);

        // Act
        var result = _service.CheckAndUnlock(wrongParentProgression, targetProgression, targetCard, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgressionService.ParentProgressionMismatch);
    }

    [Fact]
    public void CheckAndUnlock_TargetProgressionMismatch_ReturnsFailure()
    {
        // Arrange
        var parentCard = CreateTier1Card();
        var targetCard = CreateTier2Card(parentCard.Id, 5);
        var parentProgression = CreateProgression(parentCard.Id);
        var wrongTargetProgression = CreateProgression(Guid.NewGuid()); // doesn't match targetCard.Id

        // Act
        var result = _service.CheckAndUnlock(parentProgression, wrongTargetProgression, targetCard, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgressionService.TargetProgressionMismatch);
    }

    [Fact]
    public void CheckAndUnlock_RequirementsNotMet_ReturnsFailure()
    {
        // Arrange
        var parentCard = CreateTier1Card();
        var targetCard = CreateTier2Card(parentCard.Id, 10);
        var parentProgression = CreateProgression(parentCard.Id);
        var targetProgression = CreateProgression(targetCard.Id);

        // Play 5 games with parent card — need 10
        for (int i = 0; i < 5; i++)
        {
            parentProgression.RecordGameResult(isWin: false, isDraw: false);
        }

        // Act
        var result = _service.CheckAndUnlock(parentProgression, targetProgression, targetCard, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgressionService.RequirementsNotMet);
        targetProgression.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void CheckAndUnlock_RequirementsMet_UnlocksTargetProgression()
    {
        // Arrange
        var parentCard = CreateTier1Card();
        var targetCard = CreateTier2Card(parentCard.Id, 3);
        var parentProgression = CreateProgression(parentCard.Id);
        var targetProgression = CreateProgression(targetCard.Id);

        // Play 5 games with parent card — need 3
        for (int i = 0; i < 5; i++)
        {
            parentProgression.RecordGameResult(isWin: true, isDraw: false);
        }

        // Act
        var result = _service.CheckAndUnlock(parentProgression, targetProgression, targetCard, isGuestUser: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetProgression.IsUnlocked.Should().BeTrue();
        targetProgression.UnlockedAt.Should().NotBeNull();
        targetProgression.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CardUnlockedDomainEvent>();

        // Parent progression should NOT be unlocked
        parentProgression.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void CheckAndUnlock_ExactRequirements_UnlocksTargetProgression()
    {
        // Arrange
        var parentCard = CreateTier1Card();
        var targetCard = CreateTier2Card(parentCard.Id, 2);
        var parentProgression = CreateProgression(parentCard.Id);
        var targetProgression = CreateProgression(targetCard.Id);

        // Exactly 2 games with parent card
        parentProgression.RecordGameResult(isWin: true, isDraw: false);
        parentProgression.RecordGameResult(isWin: false, isDraw: true);

        // Act
        var result = _service.CheckAndUnlock(parentProgression, targetProgression, targetCard, isGuestUser: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetProgression.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public void CheckAndUnlock_AlreadyUnlocked_ReturnsFailure()
    {
        // Arrange
        var parentCard = CreateTier1Card();
        var targetCard = CreateTier2Card(parentCard.Id, 1);
        var parentProgression = CreateProgression(parentCard.Id);
        var targetProgression = CreateProgression(targetCard.Id);

        parentProgression.RecordGameResult(isWin: true, isDraw: false);
        _service.CheckAndUnlock(parentProgression, targetProgression, targetCard, isGuestUser: false);

        // Act — try to unlock again
        var result = _service.CheckAndUnlock(parentProgression, targetProgression, targetCard, isGuestUser: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgression.AlreadyUnlocked);
    }

    [Fact]
    public void CheckAndUnlock_NullParentProgression_ThrowsArgumentNullException()
    {
        // Arrange
        var targetCard = CreateTier1Card();
        var targetProgression = CreateProgression(targetCard.Id);

        // Act
        var act = () => _service.CheckAndUnlock(null!, targetProgression, targetCard, isGuestUser: false);

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("parentProgression");
    }

    [Fact]
    public void CheckAndUnlock_NullTargetProgression_ThrowsArgumentNullException()
    {
        // Arrange
        var parentProgression = CreateProgression(Guid.NewGuid());
        var targetCard = CreateTier1Card();

        // Act
        var act = () => _service.CheckAndUnlock(parentProgression, null!, targetCard, isGuestUser: false);

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("targetProgression");
    }

    [Fact]
    public void CheckAndUnlock_NullTargetCard_ThrowsArgumentNullException()
    {
        // Arrange
        var parentProgression = CreateProgression(Guid.NewGuid());
        var targetProgression = CreateProgression(Guid.NewGuid());

        // Act
        var act = () => _service.CheckAndUnlock(parentProgression, targetProgression, null!, isGuestUser: false);

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("targetCard");
    }
}
