using PrepChess.Domain.Common;
using PrepChess.Domain.ValueObjects;

namespace PrepChess.Domain.Services;

public sealed record Glicko2Update(Glicko2Rating Player1, Glicko2Rating Player2);

public static class Glicko2Calculator
{
    public const double Win = 1.0;

    public const double Draw = 0.5;

    public const double Loss = 0.0;

    public const double SystemConstant = 0.5;

    public const double MinRatingDeviation = 30.0;

    public static readonly Error NoGameResults =
        new("Glicko2Calculator.NoGameResults", "At least one game result is required.");

    public static readonly Error InvalidScore =
        new("Glicko2Calculator.InvalidScore", "Each score must be 0, 0.5, or 1.");

    private const double Scale = 173.7178;
    private const double ScaleCenter = 1500.0;

    private const double ConvergenceTolerance = 0.000001;
    private const int MaxIterations = 100;

    public static Result<Glicko2Update> Calculate(
        Glicko2Rating player1,
        Glicko2Rating player2,
        IReadOnlyList<double> player1Scores)
    {
        ArgumentNullException.ThrowIfNull(player1);
        ArgumentNullException.ThrowIfNull(player2);
        ArgumentNullException.ThrowIfNull(player1Scores);

        if (player1Scores.Count == 0)
        {
            return Result.Failure<Glicko2Update>(NoGameResults);
        }

        double player1Total = 0.0;

        foreach (double score in player1Scores)
        {
            if (score != Win && score != Draw && score != Loss)
            {
                return Result.Failure<Glicko2Update>(InvalidScore);
            }

            player1Total += score;
        }

        int games = player1Scores.Count;

        Result<Glicko2Rating> updatedPlayer1 = Update(player1, player2, games, player1Total);
        if (updatedPlayer1.IsFailure)
        {
            return Result.Failure<Glicko2Update>(updatedPlayer1.Error);
        }

        Result<Glicko2Rating> updatedPlayer2 = Update(player2, player1, games, games - player1Total);
        if (updatedPlayer2.IsFailure)
        {
            return Result.Failure<Glicko2Update>(updatedPlayer2.Error);
        }

        return Result.Success(new Glicko2Update(updatedPlayer1.Value, updatedPlayer2.Value));
    }

    public static Result<Glicko2Rating> ApplyInactivity(Glicko2Rating rating)
    {
        ArgumentNullException.ThrowIfNull(rating);

        double phi = ToDeviationScale(rating.RatingDeviation);
        double phiStar = Math.Sqrt((phi * phi) + (rating.Volatility * rating.Volatility));

        return CreateClamped(rating.Rating, FromDeviationScale(phiStar), rating.Volatility);
    }

    private static Result<Glicko2Rating> Update(
        Glicko2Rating player,
        Glicko2Rating opponent,
        int games,
        double totalScore)
    {
        double mu = ToRatingScale(player.Rating);
        double phi = ToDeviationScale(player.RatingDeviation);
        double opponentMu = ToRatingScale(opponent.Rating);
        double opponentPhi = ToDeviationScale(opponent.RatingDeviation);

        double g = G(opponentPhi);
        double expected = E(mu, opponentMu, opponentPhi);
        double variancePerGame = g * g * expected * (1.0 - expected);

        double v = 1.0 / Math.Max(games * variancePerGame, double.Epsilon);

        double outcomeSum = g * (totalScore - (games * expected));
        double delta = v * outcomeSum;

        double volatility = ComputeVolatility(phi, v, delta, player.Volatility);

        double phiStar = Math.Sqrt((phi * phi) + (volatility * volatility));
        double newPhi = 1.0 / Math.Sqrt((1.0 / (phiStar * phiStar)) + (1.0 / v));
        double newMu = mu + (newPhi * newPhi * outcomeSum);

        return CreateClamped(FromRatingScale(newMu), FromDeviationScale(newPhi), volatility);
    }

    private static double ComputeVolatility(double phi, double v, double delta, double volatility)
    {
        double priorLog = Math.Log(volatility * volatility);
        double deltaSquared = delta * delta;
        double phiSquared = phi * phi;

        double left = priorLog;
        double right;

        if (deltaSquared > phiSquared + v)
        {
            right = Math.Log(deltaSquared - phiSquared - v);
        }
        else
        {
            int k = 1;

            while (k < MaxIterations
                && VolatilityFunction(priorLog - (k * SystemConstant), priorLog, deltaSquared, phiSquared, v) < 0.0)
            {
                k++;
            }

            right = priorLog - (k * SystemConstant);
        }

        double fLeft = VolatilityFunction(left, priorLog, deltaSquared, phiSquared, v);
        double fRight = VolatilityFunction(right, priorLog, deltaSquared, phiSquared, v);

        for (int iteration = 0; iteration < MaxIterations && Math.Abs(right - left) > ConvergenceTolerance; iteration++)
        {
            double denominator = fRight - fLeft;

            if (denominator == 0.0)
            {
                break;
            }

            double next = left + ((left - right) * fLeft / denominator);
            double fNext = VolatilityFunction(next, priorLog, deltaSquared, phiSquared, v);

            if (fNext * fRight < 0.0)
            {
                left = right;
                fLeft = fRight;
            }
            else
            {
                fLeft /= 2.0;
            }

            right = next;
            fRight = fNext;
        }

        return Math.Exp(left / 2.0);
    }

    private static double VolatilityFunction(
        double x,
        double priorLog,
        double deltaSquared,
        double phiSquared,
        double v)
    {
        double exp = Math.Exp(x);
        double denominator = phiSquared + v + exp;

        return (exp * (deltaSquared - phiSquared - v - exp) / (2.0 * denominator * denominator))
            - ((x - priorLog) / (SystemConstant * SystemConstant));
    }

    private static double G(double phi) => 1.0 / Math.Sqrt(1.0 + (3.0 * phi * phi / (Math.PI * Math.PI)));

    private static double E(double mu, double opponentMu, double opponentPhi) =>
        1.0 / (1.0 + Math.Exp(-G(opponentPhi) * (mu - opponentMu)));

    private static double ToRatingScale(double rating) => (rating - ScaleCenter) / Scale;

    private static double FromRatingScale(double mu) => (Scale * mu) + ScaleCenter;

    private static double ToDeviationScale(double ratingDeviation) => ratingDeviation / Scale;

    private static double FromDeviationScale(double phi) => Scale * phi;
    private static Result<Glicko2Rating> CreateClamped(double rating, double ratingDeviation, double volatility) =>
        Glicko2Rating.Create(
            Math.Clamp(rating, Glicko2Rating.MinRating, Glicko2Rating.MaxRating),
            Math.Clamp(ratingDeviation, MinRatingDeviation, Glicko2Rating.DefaultRatingDeviation),
            Math.Clamp(volatility, double.Epsilon, Glicko2Rating.MaxVolatility));
}
