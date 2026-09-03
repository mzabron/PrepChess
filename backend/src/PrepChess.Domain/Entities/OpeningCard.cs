using PrepChess.Domain.Common;
using PrepChess.Domain.ValueObjects;

namespace PrepChess.Domain.Entities;

public sealed class OpeningCard : BaseEntity
{
    public static readonly Error EmptyTitle = new("OpeningCard.EmptyTitle", "Title cannot be empty.");
    public static readonly Error EmptyDescription = new("OpeningCard.EmptyDescription", "Description cannot be empty.");
    public static readonly Error InvalidTier = new("OpeningCard.InvalidTier", "Tier must be 1 or greater.");
    public static readonly Error Tier1MustNotHaveParent = new("OpeningCard.Tier1MustNotHaveParent", "Tier 1 cards cannot have a parent card.");
    public static readonly Error Tier1MustNotHaveUnlockRequirements = new("OpeningCard.Tier1MustNotHaveUnlockRequirements", "Tier 1 cards cannot have unlock requirements.");
    public static readonly Error HigherTierMustHaveParent = new("OpeningCard.HigherTierMustHaveParent", "Cards of tier 2 or higher must have a parent card.");
    public static readonly Error NegativeUnlockGames = new("OpeningCard.NegativeUnlockGames", "Unlock games required cannot be negative.");
    public static readonly Error EmptyEcoCode = new("OpeningCard.EmptyEcoCode", "ECO code cannot be empty.");
    public static readonly Error EmptyMoveSequence = new("OpeningCard.EmptyMoveSequence", "Move sequence cannot be empty.");
    public static readonly Error NullFen = new("OpeningCard.NullFen", "FEN cannot be null.");
    public static readonly Error NullStyleProfile = new("OpeningCard.NullStyleProfile", "Style profile cannot be null.");

    private readonly List<OpeningCard> _childCards = [];

    private OpeningCard(
        string title,
        Fen fen,
        string description,
        string ecoCode,
        string moveSequence,
        CardStyleProfile styleProfile,
        int tier,
        Guid? parentCardId,
        int unlockGamesRequired)
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
    }

#pragma warning disable CS8618 // Non-nullable properties are initialized by EF Core
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

    public OpeningCard? ParentCard { get; private set; }

    public IReadOnlyCollection<OpeningCard> ChildCards => _childCards.AsReadOnly();

    public static Result<OpeningCard> Create(
        string title,
        Fen fen,
        string description,
        string ecoCode,
        string moveSequence,
        CardStyleProfile styleProfile,
        int tier,
        Guid? parentCardId,
        int unlockGamesRequired)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<OpeningCard>(EmptyTitle);
        }

        if (fen is null)
        {
            return Result.Failure<OpeningCard>(NullFen);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<OpeningCard>(EmptyDescription);
        }

        if (string.IsNullOrWhiteSpace(ecoCode))
        {
            return Result.Failure<OpeningCard>(EmptyEcoCode);
        }

        if (string.IsNullOrWhiteSpace(moveSequence))
        {
            return Result.Failure<OpeningCard>(EmptyMoveSequence);
        }

        if (styleProfile is null)
        {
            return Result.Failure<OpeningCard>(NullStyleProfile);
        }

        if (tier < 1)
        {
            return Result.Failure<OpeningCard>(InvalidTier);
        }

        if (unlockGamesRequired < 0)
        {
            return Result.Failure<OpeningCard>(NegativeUnlockGames);
        }

        if (tier == 1)
        {
            if (parentCardId.HasValue)
            {
                return Result.Failure<OpeningCard>(Tier1MustNotHaveParent);
            }

            if (unlockGamesRequired != 0)
            {
                return Result.Failure<OpeningCard>(Tier1MustNotHaveUnlockRequirements);
            }
        }
        else if (!parentCardId.HasValue)
        {
            return Result.Failure<OpeningCard>(HigherTierMustHaveParent);
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
            unlockGamesRequired));
    }

    /// <summary>
    /// Updates the OpeningCard properties.
    /// </summary>
    public Result Update(
        string title,
        string description,
        string ecoCode,
        string moveSequence,
        CardStyleProfile styleProfile)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(EmptyTitle);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure(EmptyDescription);
        }

        if (string.IsNullOrWhiteSpace(ecoCode))
        {
            return Result.Failure(EmptyEcoCode);
        }

        if (string.IsNullOrWhiteSpace(moveSequence))
        {
            return Result.Failure(EmptyMoveSequence);
        }

        if (styleProfile is null)
        {
            return Result.Failure(NullStyleProfile);
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
