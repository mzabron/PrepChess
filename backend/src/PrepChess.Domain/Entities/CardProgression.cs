using PrepChess.Domain.Common;
using PrepChess.Domain.Events;

namespace PrepChess.Domain.Entities;

public sealed class CardProgression : BaseEntity
{
    private CardProgression(Guid userId, Guid openingCardId)
    {
        UserId = userId;
        OpeningCardId = openingCardId;
        GamesPlayed = 0;
        GamesWon = 0;
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

    public bool IsUnlocked { get; private set; }

    public DateTime? UnlockedAt { get; private set; }

    public static CardProgression Create(Guid userId, Guid openingCardId)
    {
        return new CardProgression(userId, openingCardId);
    }

    public Result RecordGameResult(bool isWin)
    {
        if (IsUnlocked)
        {
            return Result.Failure(DomainErrors.CardProgression.AlreadyUnlocked);
        }

        GamesPlayed++;

        if (isWin)
        {
            GamesWon++;
        }

        Touch();

        return Result.Success();
    }

    public Result Unlock()
    {
        if (IsUnlocked)
        {
            return Result.Failure(DomainErrors.CardProgression.AlreadyUnlocked);
        }

        IsUnlocked = true;
        UnlockedAt = DateTime.UtcNow;
        Touch();

        RaiseDomainEvent(new CardUnlockedDomainEvent(UserId, OpeningCardId));

        return Result.Success();
    }
}
