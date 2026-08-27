using System.Globalization;
using PrepChess.Domain.Common;

namespace PrepChess.Domain.ValueObjects;

public sealed record Glicko2Rating
{
    public const double DefaultRating = 1000.0;
    public const double DefaultRatingDeviation = 350.0;
    public const double DefaultVolatility = 0.06;

    public const double MinRating = 0.0;
    public const double MaxRating = 4000.0;
    public const double MinRatingDeviation = 0.0;
    public const double MaxRatingDeviation = 500.0;
    public const double MaxVolatility = 1.0;

    public static readonly Error RatingOutOfRange =
        new("Glicko2Rating.RatingOutOfRange", "Rating must be between 0 and 4000.");

    public static readonly Error RatingDeviationOutOfRange =
        new("Glicko2Rating.RatingDeviationOutOfRange", "Rating deviation must be between 0 and 500.");

    public static readonly Error VolatilityOutOfRange =
        new("Glicko2Rating.VolatilityOutOfRange", "Volatility must be greater than 0 and at most 1.");

    private Glicko2Rating(double rating, double ratingDeviation, double volatility)
    {
        Rating = rating;
        RatingDeviation = ratingDeviation;
        Volatility = volatility;
    }

    public double Rating { get; }

    public double RatingDeviation { get; }

    public double Volatility { get; }

    public static Glicko2Rating Initial { get; } =
        new(DefaultRating, DefaultRatingDeviation, DefaultVolatility);

    public int DisplayRating => (int)Math.Round(Rating - (2 * RatingDeviation));

    public bool IsProvisional => RatingDeviation > 110.0;

    public static Result<Glicko2Rating> Create(double rating, double ratingDeviation, double volatility)
    {
        if (double.IsNaN(rating) || rating < MinRating || rating > MaxRating)
        {
            return Result.Failure<Glicko2Rating>(RatingOutOfRange);
        }

        if (double.IsNaN(ratingDeviation) || ratingDeviation < MinRatingDeviation || ratingDeviation > MaxRatingDeviation)
        {
            return Result.Failure<Glicko2Rating>(RatingDeviationOutOfRange);
        }

        if (double.IsNaN(volatility) || volatility <= 0.0 || volatility > MaxVolatility)
        {
            return Result.Failure<Glicko2Rating>(VolatilityOutOfRange);
        }

        return Result.Success(new Glicko2Rating(rating, ratingDeviation, volatility));
    }

    public override string ToString() =>
        Rating.ToString("F0", CultureInfo.InvariantCulture)
        + (IsProvisional ? "?" : string.Empty);
}
