using FluentAssertions;
using PrepChess.Domain.Entities;
using PrepChess.Domain.Services;
using PrepChess.Domain.ValueObjects;
using Xunit;

namespace PrepChess.Domain.Tests.Entities;

public sealed class CardProgressionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _openingCardId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ReturnsSuccess()
    {
        var result = CardProgression.Create(_userId, _openingCardId);

        result.IsSuccess.Should().BeTrue();
        var progression = result.Value;
        progression.UserId.Should().Be(_userId);
        progression.OpeningCardId.Should().Be(_openingCardId);
        progression.GamesPlayed.Should().Be(0);
        progression.GamesWon.Should().Be(0);
        progression.GamesDrawn.Should().Be(0);
        progression.IsUnlocked.Should().BeFalse();
        progression.UnlockedAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyUserId_ReturnsFailure()
    {
        var result = CardProgression.Create(Guid.Empty, _openingCardId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgression.EmptyUserId);
    }

    [Fact]
    public void Create_EmptyOpeningCardId_ReturnsFailure()
    {
        var result = CardProgression.Create(_userId, Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgression.EmptyOpeningCardId);
    }

    [Fact]
    public void RecordGameResult_Win_IncrementsBothCounters()
    {
        var progression = CardProgression.Create(_userId, _openingCardId).Value;

        var result = progression.RecordGameResult(isWin: true, isDraw: false);

        result.IsSuccess.Should().BeTrue();
        progression.GamesPlayed.Should().Be(1);
        progression.GamesWon.Should().Be(1);
        progression.GamesDrawn.Should().Be(0);
    }

    [Fact]
    public void RecordGameResult_Loss_IncrementsOnlyGamesPlayed()
    {
        var progression = CardProgression.Create(_userId, _openingCardId).Value;

        var result = progression.RecordGameResult(isWin: false, isDraw: false);

        result.IsSuccess.Should().BeTrue();
        progression.GamesPlayed.Should().Be(1);
        progression.GamesWon.Should().Be(0);
        progression.GamesDrawn.Should().Be(0);
    }

    [Fact]
    public void RecordGameResult_Draw_IncrementsGamesPlayedAndGamesDrawn()
    {
        var progression = CardProgression.Create(_userId, _openingCardId).Value;

        var result = progression.RecordGameResult(isWin: false, isDraw: true);

        result.IsSuccess.Should().BeTrue();
        progression.GamesPlayed.Should().Be(1);
        progression.GamesWon.Should().Be(0);
        progression.GamesDrawn.Should().Be(1);
    }

    [Fact]
    public void RecordGameResult_WinAndDraw_ReturnsFailure()
    {
        var progression = CardProgression.Create(_userId, _openingCardId).Value;

        var result = progression.RecordGameResult(isWin: true, isDraw: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CardProgression.InvalidGameResult);
        progression.GamesPlayed.Should().Be(0);
    }

    [Fact]
    public void RecordGameResult_MultipleCalls_AccumulatesCorrectly()
    {
        var progression = CardProgression.Create(_userId, _openingCardId).Value;

        progression.RecordGameResult(isWin: true, isDraw: false);
        progression.RecordGameResult(isWin: false, isDraw: false);
        progression.RecordGameResult(isWin: false, isDraw: true);
        progression.RecordGameResult(isWin: true, isDraw: false);

        progression.GamesPlayed.Should().Be(4);
        progression.GamesWon.Should().Be(2);
        progression.GamesDrawn.Should().Be(1);
    }

    [Fact]
    public void RecordGameResult_AfterUnlock_StillRecords()
    {
        var styleProfile = CardStyleProfile.Create(1, 1, 1).Value;
        var fen = Fen.Create("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1").Value;
        
        var parentCard = OpeningCard.Create("Parent", fen, "Desc", "A00", "e4", styleProfile, 1, null, 0).Value;
        var targetCard = OpeningCard.Create("Target", fen, "Desc", "A00", "e4", styleProfile, 2, parentCard.Id, 1).Value;
        
        var parentProgression = CardProgression.Create(_userId, parentCard.Id).Value;
        var targetProgression = CardProgression.Create(_userId, targetCard.Id).Value;

        parentProgression.RecordGameResult(isWin: true, isDraw: false); 

        var service = new CardProgressionService();
        service.CheckAndUnlock(parentProgression, targetProgression, targetCard, isGuestUser: false).IsSuccess.Should().BeTrue();

        var result = targetProgression.RecordGameResult(isWin: false, isDraw: false);

        result.IsSuccess.Should().BeTrue();
        targetProgression.GamesPlayed.Should().Be(1); 
    }
}
