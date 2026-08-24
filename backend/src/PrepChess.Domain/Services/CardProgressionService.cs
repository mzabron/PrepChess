using PrepChess.Domain.Common;
using PrepChess.Domain.Entities;

namespace PrepChess.Domain.Services;

/// <summary>
/// Pure domain service that checks unlock eligibility and progresses card unlocks.
/// Enforces the rule that guest players cannot participate in progression.
/// </summary>
public sealed class CardProgressionService
{
    /// <summary>
    /// Checks whether a card progression meets the unlock requirements of the target
    /// opening card, and unlocks it if eligible.
    /// </summary>
    /// <param name="progression">The user's progression record for the target card.</param>
    /// <param name="targetCard">The opening card to potentially unlock.</param>
    /// <param name="isGuestUser">Whether the user is a guest (guests cannot progress).</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success if the card was unlocked,
    /// or failure with a descriptive error if progression is not allowed or requirements are not met.
    /// </returns>
    public Result CheckAndUnlock(CardProgression progression, OpeningCard targetCard, bool isGuestUser)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(targetCard);

        if (isGuestUser)
        {
            return Result.Failure(DomainErrors.CardProgressionService.GuestCannotProgress);
        }

        if (progression.IsUnlocked)
        {
            return Result.Failure(DomainErrors.CardProgression.AlreadyUnlocked);
        }

        if (targetCard.Tier == 1)
        {
            return Result.Failure(DomainErrors.CardProgressionService.Tier1AlwaysUnlocked);
        }

        bool gamesRequirementMet = progression.GamesPlayed >= targetCard.UnlockGamesRequired;
        bool winsRequirementMet = progression.GamesWon >= targetCard.UnlockWinsRequired;

        if (!gamesRequirementMet || !winsRequirementMet)
        {
            return Result.Failure(DomainErrors.CardProgressionService.RequirementsNotMet);
        }

        return progression.Unlock();
    }
}
