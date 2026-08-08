using PrepChess.Domain.Common;

namespace PrepChess.Domain.ValueObjects;

public sealed record CardStyleProfile
{
    private const double Min = 0.0;
    private const double Max = 100.0;

    public static readonly Error ScoreOutOfRange =
        new("CardStyleProfile.ScoreOutOfRange", "Axis scores must be between 0 and 100.");

    private CardStyleProfile(double tacticalPositional, double theoreticalIntuitive, double easyHard)
    {
        TacticalPositional = tacticalPositional;
        TheoreticalIntuitive = theoreticalIntuitive;
        EasyHard = easyHard;
    }

    // 0 = Tactical, 100 = Positional
    public double TacticalPositional { get; }

    // 0 = Theoretical, 100 = Intuitive
    public double TheoreticalIntuitive { get; }

    // 0 = Easy, 100 = Hard
    public double EasyHard { get; }

    public static Result<CardStyleProfile> Create(
        double tacticalPositional,
        double theoreticalIntuitive,
        double easyHard)
    {
        if (!IsInRange(tacticalPositional) || !IsInRange(theoreticalIntuitive) || !IsInRange(easyHard))
        {
            return Result.Failure<CardStyleProfile>(ScoreOutOfRange);
        }

        return Result.Success(new CardStyleProfile(tacticalPositional, theoreticalIntuitive, easyHard));
    }

    private static bool IsInRange(double score) => !double.IsNaN(score) && score is >= Min and <= Max;
}
