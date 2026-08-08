using System.Globalization;
using PrepChess.Domain.Common;

namespace PrepChess.Domain.ValueObjects;

public sealed record MatchScore
{
    private const decimal Win = 1.0m;
    private const decimal DrawShare = 0.5m;

    public static readonly Error Negative =
        new("MatchScore.Negative", "Scores cannot be negative.");

    public static readonly Error NotAHalfPoint =
        new("MatchScore.NotAHalfPoint", "Scores must be whole or half points.");

    private MatchScore(decimal player1Score, decimal player2Score)
    {
        Player1Score = player1Score;
        Player2Score = player2Score;
    }

    public decimal Player1Score { get; }

    public decimal Player2Score { get; }

    public static MatchScore Zero { get; } = new(0m, 0m);

    public bool IsTied => Player1Score == Player2Score;

    public MatchScore AwardPlayer1Win() => new(Player1Score + Win, Player2Score);

    public MatchScore AwardPlayer2Win() => new(Player1Score, Player2Score + Win);

    public MatchScore AwardDraw() => new(Player1Score + DrawShare, Player2Score + DrawShare);

    public static Result<MatchScore> Create(decimal player1Score, decimal player2Score)
    {
        if (player1Score < 0m || player2Score < 0m)
        {
            return Result.Failure<MatchScore>(Negative);
        }

        if (!IsHalfPoint(player1Score) || !IsHalfPoint(player2Score))
        {
            return Result.Failure<MatchScore>(NotAHalfPoint);
        }

        return Result.Success(new MatchScore(player1Score, player2Score));
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Player1Score:0.#}-{Player2Score:0.#}");

    private static bool IsHalfPoint(decimal score) => score % DrawShare == 0m;
}
