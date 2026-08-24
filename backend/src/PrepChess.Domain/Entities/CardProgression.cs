using PrepChess.Domain.Common;
using PrepChess.Domain.Events;

namespace PrepChess.Domain.Entities;

/// <summary>
/// Tracks a user's progression toward unlocking a specific opening card.
/// Each record represents one (user, card) pair tracking games played and won
/// with the parent opening.
/// </summary>
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

    /// <summary>
    /// EF Core materialization constructor. Do not use from application code.
    /// </summary>
    private CardProgression()
    {
    }

    /// <summary>The user whose progression this record tracks.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The opening card being progressed toward.</summary>
    public Guid OpeningCardId { get; private set; }

    /// <summary>Number of games played with the parent opening.</summary>
    public int GamesPlayed { get; private set; }

    /// <summary>Number of games won with the parent opening.</summary>
    public int GamesWon { get; private set; }

    /// <summary>Whether the card has been unlocked.</summary>
    public bool IsUnlocked { get; private set; }

    /// <summary>Timestamp when the card was unlocked. Null if not yet unlocked.</summary>
    public DateTime? UnlockedAt { get; private set; }

    /// <summary>
    /// Creates a new card progression record for a user and opening card.
    /// </summary>
    /// <param name="userId">The user whose progression to track.</param>
    /// <param name="openingCardId">The opening card to progress toward.</param>
    /// <returns>A new <see cref="CardProgression"/> with zero counters.</returns>
    public static CardProgression Create(Guid userId, Guid openingCardId)
    {
        return new CardProgression(userId, openingCardId);
    }

    /// <summary>
    /// Records the result of a game played with the parent opening.
    /// Increments the games played counter, and also the games won counter if the game was won.
    /// </summary>
    /// <param name="isWin">Whether the game was won.</param>
    /// <returns>A <see cref="Result"/> indicating success, or failure if the card is already unlocked.</returns>
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

    /// <summary>
    /// Marks this card as unlocked and raises a <see cref="CardUnlockedDomainEvent"/>.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success, or failure if already unlocked.</returns>
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
