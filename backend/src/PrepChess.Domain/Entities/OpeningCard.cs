using PrepChess.Domain.Common;
using PrepChess.Domain.ValueObjects;

namespace PrepChess.Domain.Entities;

/// <summary>
/// A global opening card representing a chess opening that players can use in their decks.
/// Forms a progression tree: Tier 1 cards are free, higher-tier cards are unlocked by
/// playing games with the parent opening.
/// </summary>
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

    /// <summary>
    /// EF Core materialization constructor. Do not use from application code.
    /// </summary>
#pragma warning disable CS8618 // Required properties are set by EF Core via backing fields
    private OpeningCard()
    {
    }
#pragma warning restore CS8618

    /// <summary>The display name of the opening (e.g. "Queen's Gambit").</summary>
    public string Title { get; private set; }

    /// <summary>The board position after the opening moves.</summary>
    public Fen Fen { get; private set; }

    /// <summary>A human-readable description of the opening's character and strategy.</summary>
    public string Description { get; private set; }

    /// <summary>The ECO classification code (e.g. "D06").</summary>
    public string EcoCode { get; private set; }

    /// <summary>SAN notation of the moves leading to the FEN (e.g. "1.d4 d5 2.c4").</summary>
    public string MoveSequence { get; private set; }

    /// <summary>Three-axis style profile: Tactical↔Positional, Theoretical↔Intuitive, Easy↔Hard.</summary>
    public CardStyleProfile StyleProfile { get; private set; }

    /// <summary>Progression tier: 1 = starter (free), 2+ = unlockable.</summary>
    public int Tier { get; private set; }

    /// <summary>The parent card in the progression tree. Null for Tier 1 root cards.</summary>
    public Guid? ParentCardId { get; private set; }

    /// <summary>Number of games in the parent opening required to unlock this card.</summary>
    public int UnlockGamesRequired { get; private set; }

    /// <summary>Number of wins in the parent opening required to unlock this card.</summary>
    public int UnlockWinsRequired { get; private set; }

    /// <summary>Navigation property to the parent card. Null for Tier 1 root cards.</summary>
    public OpeningCard? ParentCard { get; private set; }

    /// <summary>
    /// Creates a new opening card with full validation.
    /// </summary>
    /// <param name="title">The display name of the opening.</param>
    /// <param name="fen">The board position after the opening moves.</param>
    /// <param name="description">A human-readable description of the opening.</param>
    /// <param name="ecoCode">The ECO classification code.</param>
    /// <param name="moveSequence">SAN notation of the moves leading to the FEN.</param>
    /// <param name="styleProfile">Three-axis style profile for UI display.</param>
    /// <param name="tier">Progression tier (1 = free, 2+ = unlockable).</param>
    /// <param name="parentCardId">The parent card ID. Null for Tier 1 root cards.</param>
    /// <param name="unlockGamesRequired">Games in parent opening required to unlock.</param>
    /// <param name="unlockWinsRequired">Wins in parent opening required to unlock.</param>
    /// <returns>A <see cref="Result{T}"/> containing the created card or a validation error.</returns>
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

    /// <summary>
    /// Updates the mutable properties of an existing opening card.
    /// </summary>
    /// <param name="title">The new display name.</param>
    /// <param name="description">The new description.</param>
    /// <param name="ecoCode">The new ECO code.</param>
    /// <param name="moveSequence">The new SAN move sequence.</param>
    /// <param name="styleProfile">The new style profile.</param>
    /// <returns>A <see cref="Result"/> indicating success or a validation error.</returns>
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
