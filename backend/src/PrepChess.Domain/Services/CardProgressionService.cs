using PrepChess.Domain.Common;
using PrepChess.Domain.Entities;

namespace PrepChess.Domain.Services;

public sealed class CardProgressionService
{
    public static readonly Error GuestCannotProgress = new("CardProgressionService.GuestCannotProgress", "Guests cannot progress cards.");
    public static readonly Error RequirementsNotMet = new("CardProgressionService.RequirementsNotMet", "Unlock requirements have not been met yet.");
    public static readonly Error ParentProgressionMismatch = new("CardProgressionService.ParentProgressionMismatch", "The parent progression does not track the target card's parent opening.");
    public static readonly Error TargetProgressionMismatch = new("CardProgressionService.TargetProgressionMismatch", "The target progression does not track the card being unlocked.");

    public Result CheckAndUnlock(
        CardProgression parentProgression,
        CardProgression targetProgression,
        OpeningCard targetCard,
        bool isGuestUser)
    {
        ArgumentNullException.ThrowIfNull(parentProgression);
        ArgumentNullException.ThrowIfNull(targetProgression);
        ArgumentNullException.ThrowIfNull(targetCard);

        if (isGuestUser)
        {
            return Result.Failure(GuestCannotProgress);
        }

        if (targetCard.Tier == 1)
        {
            return Result.Success();
        }

        if (parentProgression.OpeningCardId != targetCard.ParentCardId)
        {
            return Result.Failure(ParentProgressionMismatch);
        }

        if (targetProgression.OpeningCardId != targetCard.Id)
        {
            return Result.Failure(TargetProgressionMismatch);
        }

        if (parentProgression.GamesPlayed < targetCard.UnlockGamesRequired)
        {
            return Result.Failure(RequirementsNotMet);
        }

        return targetProgression.Unlock();
    }
}
