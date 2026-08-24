using PrepChess.Domain.Common;
using PrepChess.Domain.Entities;

namespace PrepChess.Domain.Services;

public sealed class CardProgressionService
{
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
