using FluentAssertions;
using PrepChess.Domain.Common;
using PrepChess.Domain.Entities;
using PrepChess.Domain.ValueObjects;
using Xunit;

namespace PrepChess.Domain.Tests.Entities;

public sealed class OpeningCardTests
{
    private static readonly Fen ValidFen = Fen.Create("rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR b KQkq d3 0 1").Value;
    private static readonly CardStyleProfile ValidProfile = CardStyleProfile.Create(40, 60, 30).Value;

    // --- Create: Tier 1 (root) success ---

    [Fact]
    public void Create_Tier1WithValidData_ReturnsSuccessResult()
    {
        // Act
        var result = OpeningCard.Create(
            "Queen's Gambit",
            ValidFen,
            "A solid 1.d4 opening",
            "D06",
            "1.d4 d5 2.c4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Queen's Gambit");
        result.Value.Fen.Should().Be(ValidFen);
        result.Value.Tier.Should().Be(1);
        result.Value.ParentCardId.Should().BeNull();
        result.Value.UnlockGamesRequired.Should().Be(0);
        result.Value.UnlockWinsRequired.Should().Be(0);
        result.Value.StyleProfile.Should().Be(ValidProfile);
    }

    // --- Create: Tier 2+ success ---

    [Fact]
    public void Create_Tier2WithValidParent_ReturnsSuccessResult()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var result = OpeningCard.Create(
            "QG Declined",
            ValidFen,
            "The classical defense",
            "D30",
            "1.d4 d5 2.c4 e6",
            ValidProfile,
            tier: 2,
            parentCardId: parentId,
            unlockGamesRequired: 20,
            unlockWinsRequired: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tier.Should().Be(2);
        result.Value.ParentCardId.Should().Be(parentId);
        result.Value.UnlockGamesRequired.Should().Be(20);
        result.Value.UnlockWinsRequired.Should().Be(5);
    }

    // --- Create: validation failures ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ReturnsFailure(string? title)
    {
        // Act
        var result = OpeningCard.Create(
            title!,
            ValidFen,
            "Description",
            "D06",
            "1.d4 d5 2.c4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.EmptyTitle);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyDescription_ReturnsFailure(string? description)
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            description!,
            "D06",
            "1.d4 d5 2.c4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.EmptyDescription);
    }

    [Fact]
    public void Create_WithTierZero_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 0,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.InvalidTier);
    }

    [Fact]
    public void Create_Tier1WithParentCard_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 1,
            parentCardId: Guid.NewGuid(),
            unlockGamesRequired: 0,
            unlockWinsRequired: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.Tier1MustNotHaveParent);
    }

    [Fact]
    public void Create_Tier1WithNonZeroUnlockGames_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 10,
            unlockWinsRequired: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.Tier1MustNotHaveUnlockRequirements);
    }

    [Fact]
    public void Create_Tier1WithNonZeroUnlockWins_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 5,
            unlockWinsRequired: 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.Tier1MustNotHaveUnlockRequirements);
    }

    [Fact]
    public void Create_Tier2WithoutParentCard_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 2,
            parentCardId: null,
            unlockGamesRequired: 20,
            unlockWinsRequired: 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.HigherTierMustHaveParent);
    }

    [Fact]
    public void Create_WithNegativeUnlockGames_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 2,
            parentCardId: Guid.NewGuid(),
            unlockGamesRequired: -1,
            unlockWinsRequired: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.NegativeUnlockGames);
    }

    [Fact]
    public void Create_WithNegativeUnlockWins_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 2,
            parentCardId: Guid.NewGuid(),
            unlockGamesRequired: 10,
            unlockWinsRequired: -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.NegativeUnlockWins);
    }

    [Fact]
    public void Create_WithUnlockWinsExceedingGames_ReturnsFailure()
    {
        // Act
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 2,
            parentCardId: Guid.NewGuid(),
            unlockGamesRequired: 10,
            unlockWinsRequired: 15);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.UnlockWinsExceedGames);
    }

    // --- Update ---

    [Fact]
    public void Update_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var card = OpeningCard.Create(
            "Original",
            ValidFen,
            "Original description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0).Value;

        var newProfile = CardStyleProfile.Create(80, 20, 70).Value;

        // Act
        var result = card.Update("Updated", "New description", "D07", "1.d4 d5", newProfile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        card.Title.Should().Be("Updated");
        card.Description.Should().Be("New description");
        card.EcoCode.Should().Be("D07");
        card.MoveSequence.Should().Be("1.d4 d5");
        card.StyleProfile.Should().Be(newProfile);
    }

    [Fact]
    public void Update_WithEmptyTitle_ReturnsFailure()
    {
        // Arrange
        var card = OpeningCard.Create(
            "Original",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0).Value;

        // Act
        var result = card.Update("", "Description", "D06", "1.d4", ValidProfile);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.EmptyTitle);
    }

    [Fact]
    public void Update_WithEmptyDescription_ReturnsFailure()
    {
        // Arrange
        var card = OpeningCard.Create(
            "Original",
            ValidFen,
            "Description",
            "D06",
            "1.d4",
            ValidProfile,
            tier: 1,
            parentCardId: null,
            unlockGamesRequired: 0,
            unlockWinsRequired: 0).Value;

        // Act
        var result = card.Update("Title", "", "D06", "1.d4", ValidProfile);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.OpeningCard.EmptyDescription);
    }
}
