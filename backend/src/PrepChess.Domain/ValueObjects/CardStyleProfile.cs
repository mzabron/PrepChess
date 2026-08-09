using PrepChess.Domain.Common;

namespace PrepChess.Domain.ValueObjects;

public sealed record CardStyleProfile
{
    private const int Min = 0;
    private const int Max = 100;

    public static readonly Error ScoreOutOfRange =
        new("CardStyleProfile.ScoreOutOfRange", "Axis scores must be between 0 and 100.");

    private CardStyleProfile(int tacticalPositional, int theoreticalIntuitive, int easyHard)
    {
        TacticalPositional = tacticalPositional;
        TheoreticalIntuitive = theoreticalIntuitive;
        EasyHard = easyHard;
    }

    // 0 = Tactical, 100 = Positional
    public int TacticalPositional { get; }

    // 0 = Theoretical, 100 = Intuitive
    public int TheoreticalIntuitive { get; }

    // 0 = Easy, 100 = Hard
    public int EasyHard { get; }

    public static Result<CardStyleProfile> Create(
        int tacticalPositional,
        int theoreticalIntuitive,
        int easyHard)
    {
        if (!IsInRange(tacticalPositional) || !IsInRange(theoreticalIntuitive) || !IsInRange(easyHard))
        {
            return Result.Failure<CardStyleProfile>(ScoreOutOfRange);
        }

        return Result.Success(new CardStyleProfile(tacticalPositional, theoreticalIntuitive, easyHard));
    }

    private static bool IsInRange(int score) => score is >= Min and <= Max;
}
