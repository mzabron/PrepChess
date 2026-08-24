namespace PrepChess.Domain.Common;

public static class DomainErrors
{
    public static class OpeningCard
    {
        public static readonly Error EmptyTitle =
            new("OpeningCard.EmptyTitle", "Title is required.");

        public static readonly Error EmptyDescription =
            new("OpeningCard.EmptyDescription", "Description is required.");

        public static readonly Error InvalidTier =
            new("OpeningCard.InvalidTier", "Tier must be at least 1.");

        public static readonly Error Tier1MustNotHaveParent =
            new("OpeningCard.Tier1MustNotHaveParent", "Tier 1 cards cannot have a parent card.");

        public static readonly Error Tier1MustNotHaveUnlockRequirements =
            new("OpeningCard.Tier1MustNotHaveUnlockRequirements", "Tier 1 cards must have zero unlock requirements.");

        public static readonly Error HigherTierMustHaveParent =
            new("OpeningCard.HigherTierMustHaveParent", "Cards above Tier 1 must have a parent card.");

        public static readonly Error NegativeUnlockGames =
            new("OpeningCard.NegativeUnlockGames", "Unlock games required cannot be negative.");

        public static readonly Error NegativeUnlockWins =
            new("OpeningCard.NegativeUnlockWins", "Unlock wins required cannot be negative.");

        public static readonly Error UnlockWinsExceedGames =
            new("OpeningCard.UnlockWinsExceedGames", "Unlock wins required cannot exceed unlock games required.");
    }

    public static class CardProgression
    {
        public static readonly Error AlreadyUnlocked =
            new("CardProgression.AlreadyUnlocked", "This card has already been unlocked.");
    }

    public static class CardProgressionService
    {
        public static readonly Error GuestCannotProgress =
            new("CardProgressionService.GuestCannotProgress", "Guest players cannot participate in card progression.");

        public static readonly Error Tier1AlwaysUnlocked =
            new("CardProgressionService.Tier1AlwaysUnlocked", "Tier 1 cards are always unlocked and do not require progression.");

        public static readonly Error RequirementsNotMet =
            new("CardProgressionService.RequirementsNotMet", "Unlock requirements have not been met yet.");
    }
}
