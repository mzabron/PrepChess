using PrepChess.Domain.Common;
using PrepChess.Domain.Events;

namespace PrepChess.Domain.Entities;

public sealed class CardProgression : BaseEntity
{
    public static readonly Error AlreadyUnlocked = new("CardProgression.AlreadyUnlocked", "Card is already unlocked.");
    public static readonly Error EmptyUserId = new("CardProgression.EmptyUserId", "User ID cannot be empty.");
    public static readonly Error EmptyOpeningCardId = new("CardProgression.EmptyOpeningCardId", "Opening Card ID cannot be empty.");
    public static readonly Error InvalidGameResult = new("CardProgression.InvalidGameResult", "A game result cannot be both a win and a draw.");

    private CardProgression(Guid userId, Guid openingCardId)
    {
        UserId = userId;
        OpeningCardId = openingCardId;
        GamesPlayed = 0;
        GamesWon = 0;
        GamesDrawn = 0;
        IsUnlocked = false;
        UnlockedAt = null;
    }

    private CardProgression()
    {
    }

    public Guid UserId { get; private set; }

    public Guid OpeningCardId { get; private set; }

    public int GamesPlayed { get; private set; }

    public int GamesWon { get; private set; }
    
    public int GamesDrawn { get; private set; }

    public bool IsUnlocked { get; private set; }

    public DateTime? UnlockedAt { get; private set; }

    public static Result<CardProgression> Create(Guid userId, Guid openingCardId)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<CardProgression>(EmptyUserId);
        }

        if (openingCardId == Guid.Empty)
        {
            return Result.Failure<CardProgression>(EmptyOpeningCardId);
        }

        return Result.Success(new CardProgression(userId, openingCardId));
    }

    public Result RecordGameResult(bool isWin, bool isDraw)
    {
        if (isWin && isDraw)
        {
            return Result.Failure(InvalidGameResult);
        }

        GamesPlayed++;

        if (isWin)
        {
            GamesWon++;
        }
        else if (isDraw)
        {
            GamesDrawn++;
        }

        Touch();

        return Result.Success();
    }

    internal Result Unlock()
    {
        if (IsUnlocked)
        {
            return Result.Failure(AlreadyUnlocked);
        }

        IsUnlocked = true;
        UnlockedAt = DateTime.UtcNow;
        Touch();

        RaiseDomainEvent(new CardUnlockedDomainEvent(UserId, OpeningCardId));

        return Result.Success();
    }
}
