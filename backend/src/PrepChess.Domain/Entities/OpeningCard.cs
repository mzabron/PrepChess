using PrepChess.Domain.Common;
using PrepChess.Domain.ValueObjects;

namespace PrepChess.Domain.Entities;

public sealed class OpeningCard : BaseEntity
{
    private OpeningCard(
        string title,
        Fen fen,
        string description,
        string ecoCode,
        string moveSequence,
        CardStyleProfile styleProfile,
        int tier,
        Guid? parentCardId,
        int unlockGamesRequired,
        int unlockWinsRequired)
    {
        Title = title;
        Fen = fen;
        Description = description;
        EcoCode = ecoCode;
        MoveSequence = moveSequence;
        StyleProfile = styleProfile;
        Tier = tier;
        ParentCardId = parentCardId;
        UnlockGamesRequired = unlockGamesRequired;
        UnlockWinsRequired = unlockWinsRequired;
    }

#pragma warning disable CS8618 // Required properties are set by EF Core via backing fields
    private OpeningCard()
    {
    }
#pragma warning restore CS8618

    public string Title { get; private set; }

    public Fen Fen { get; private set; }

    public string Description { get; private set; }

    public string EcoCode { get; private set; }

    public string MoveSequence { get; private set; }

    public CardStyleProfile StyleProfile { get; private set; }

    public int Tier { get; private set; }

    public Guid? ParentCardId { get; private set; }

    public int UnlockGamesRequired { get; private set; }

    public int UnlockWinsRequired { get; private set; }

    public OpeningCard? ParentCard { get; private set; }

    public static Result<OpeningCard> Create(
        string title,
        Fen fen,
        string description,
        string ecoCode,
        string moveSequence,
        CardStyleProfile styleProfile,
        int tier,
        Guid? parentCardId,
        int unlockGamesRequired,
        int unlockWinsRequired)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.EmptyTitle);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.EmptyDescription);
        }

        if (tier < 1)
        {
            return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.InvalidTier);
        }

        if (unlockGamesRequired < 0)
        {
            return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.NegativeUnlockGames);
        }

        if (unlockWinsRequired < 0)
        {
            return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.NegativeUnlockWins);
        }

        if (unlockWinsRequired > unlockGamesRequired)
        {
            return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.UnlockWinsExceedGames);
        }

        if (tier == 1)
        {
            if (parentCardId.HasValue)
            {
                return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.Tier1MustNotHaveParent);
            }

            if (unlockGamesRequired != 0 || unlockWinsRequired != 0)
            {
                return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.Tier1MustNotHaveUnlockRequirements);
            }
        }
        else if (!parentCardId.HasValue)
        {
            return Result.Failure<OpeningCard>(DomainErrors.OpeningCard.HigherTierMustHaveParent);
        }

        return Result.Success(new OpeningCard(
            title,
            fen,
            description,
            ecoCode,
            moveSequence,
            styleProfile,
            tier,
            parentCardId,
            unlockGamesRequired,
            unlockWinsRequired));
    }

    public Result Update(
        string title,
        string description,
        string ecoCode,
        string moveSequence,
        CardStyleProfile styleProfile)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(DomainErrors.OpeningCard.EmptyTitle);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure(DomainErrors.OpeningCard.EmptyDescription);
        }

        Title = title;
        Description = description;
        EcoCode = ecoCode;
        MoveSequence = moveSequence;
        StyleProfile = styleProfile;
        Touch();

        return Result.Success();
    }
}
