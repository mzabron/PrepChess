using FluentAssertions;
using PrepChess.Domain.Entities;
using PrepChess.Domain.ValueObjects;
using Xunit;

namespace PrepChess.Domain.Tests.Entities;

public sealed class OpeningCardTests
{
    private static readonly Fen ValidFen = Fen.Create("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1").Value;
    private static readonly CardStyleProfile ValidStyleProfile = CardStyleProfile.Create(1, 1, 1).Value;

    [Fact]
    public void Create_ValidDataTier1_ReturnsSuccess()
    {
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "A00",
            "e4",
            ValidStyleProfile,
            1,
            null,
            0);

        result.IsSuccess.Should().BeTrue();
        var card = result.Value;
        card.Title.Should().Be("Title");
        card.Fen.Should().Be(ValidFen);
        card.Description.Should().Be("Description");
        card.EcoCode.Should().Be("A00");
        card.MoveSequence.Should().Be("e4");
        card.StyleProfile.Should().Be(ValidStyleProfile);
        card.Tier.Should().Be(1);
        card.ParentCardId.Should().BeNull();
        card.UnlockGamesRequired.Should().Be(0);
    }

    [Fact]
    public void Create_ValidDataTier2_ReturnsSuccess()
    {
        var parentId = Guid.NewGuid();
        var result = OpeningCard.Create(
            "Title",
            ValidFen,
            "Description",
            "A00",
            "e4",
            ValidStyleProfile,
            2,
            parentId,
            10);

        result.IsSuccess.Should().BeTrue();
        var card = result.Value;
        card.Tier.Should().Be(2);
        card.ParentCardId.Should().Be(parentId);
        card.UnlockGamesRequired.Should().Be(10);
    }

    [Fact]
    public void Create_EmptyTitle_ReturnsFailure()
    {
        var result = OpeningCard.Create("", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyTitle);
    }

    [Fact]
    public void Create_NullFen_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", null!, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.NullFen);
    }

    [Fact]
    public void Create_EmptyDescription_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "", "A00", "e4", ValidStyleProfile, 1, null, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyDescription);
    }

    [Fact]
    public void Create_EmptyEcoCode_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "", "e4", ValidStyleProfile, 1, null, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyEcoCode);
    }

    [Fact]
    public void Create_EmptyMoveSequence_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "", ValidStyleProfile, 1, null, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyMoveSequence);
    }

    [Fact]
    public void Create_NullStyleProfile_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", null!, 1, null, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.NullStyleProfile);
    }

    [Fact]
    public void Create_InvalidTier_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 0, null, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.InvalidTier);
    }

    [Fact]
    public void Create_NegativeUnlockGames_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 2, Guid.NewGuid(), -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.NegativeUnlockGames);
    }

    [Fact]
    public void Create_Tier1WithParent_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, Guid.NewGuid(), 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.Tier1MustNotHaveParent);
    }

    [Fact]
    public void Create_Tier1WithUnlockRequirements_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.Tier1MustNotHaveUnlockRequirements);
    }

    [Fact]
    public void Create_HigherTierWithoutParent_ReturnsFailure()
    {
        var result = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 2, null, 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.HigherTierMustHaveParent);
    }

    [Fact]
    public void Update_ValidData_ReturnsSuccess()
    {
        var card = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0).Value;

        var result = card.Update("New Title", "New Desc", "B00", "e4 e5", ValidStyleProfile);

        result.IsSuccess.Should().BeTrue();
        card.Title.Should().Be("New Title");
        card.Description.Should().Be("New Desc");
        card.EcoCode.Should().Be("B00");
        card.MoveSequence.Should().Be("e4 e5");
    }

    [Fact]
    public void Update_EmptyTitle_ReturnsFailure()
    {
        var card = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0).Value;

        var result = card.Update("", "New Desc", "B00", "e4 e5", ValidStyleProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyTitle);
    }

    [Fact]
    public void Update_EmptyDescription_ReturnsFailure()
    {
        var card = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0).Value;

        var result = card.Update("New Title", "", "B00", "e4 e5", ValidStyleProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyDescription);
    }

    [Fact]
    public void Update_EmptyEcoCode_ReturnsFailure()
    {
        var card = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0).Value;

        var result = card.Update("New Title", "New Desc", "", "e4 e5", ValidStyleProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyEcoCode);
    }

    [Fact]
    public void Update_EmptyMoveSequence_ReturnsFailure()
    {
        var card = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0).Value;

        var result = card.Update("New Title", "New Desc", "B00", "", ValidStyleProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.EmptyMoveSequence);
    }

    [Fact]
    public void Update_NullStyleProfile_ReturnsFailure()
    {
        var card = OpeningCard.Create("Title", ValidFen, "Desc", "A00", "e4", ValidStyleProfile, 1, null, 0).Value;

        var result = card.Update("New Title", "New Desc", "B00", "e4 e5", null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OpeningCard.NullStyleProfile);
    }
}
