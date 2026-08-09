using System.Globalization;
using PrepChess.Domain.Common;

namespace PrepChess.Domain.ValueObjects;

public sealed record TimeControl
{
    public static readonly Error MinutesOutOfRange =
        new("TimeControl.MinutesOutOfRange", "Minutes must be between 1 and 180.");

    public static readonly Error IncrementOutOfRange =
        new("TimeControl.IncrementOutOfRange", "Increment must be between 0 and 60 seconds.");

    private TimeControl(int minutes, int incrementSeconds)
    {
        Minutes = minutes;
        IncrementSeconds = incrementSeconds;
    }

    public int Minutes { get; }

    public int IncrementSeconds { get; }

    public static TimeControl Blitz { get; } = new(3, 0);

    public static TimeControl Rapid { get; } = new(10, 0);

    public TimeSpan InitialTime => TimeSpan.FromMinutes(Minutes);

    public TimeSpan Increment => TimeSpan.FromSeconds(IncrementSeconds);

    public static Result<TimeControl> Create(int minutes, int incrementSeconds)
    {
        if (minutes is < 1 or > 180)
        {
            return Result.Failure<TimeControl>(MinutesOutOfRange);
        }

        if (incrementSeconds is < 0 or > 60)
        {
            return Result.Failure<TimeControl>(IncrementOutOfRange);
        }

        return Result.Success(new TimeControl(minutes, incrementSeconds));
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Minutes}+{IncrementSeconds}");
}
