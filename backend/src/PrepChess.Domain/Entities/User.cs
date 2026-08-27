using PrepChess.Domain.Common;
using PrepChess.Domain.Enums;
using PrepChess.Domain.ValueObjects;

namespace PrepChess.Domain.Entities;

public sealed class User : AggregateRoot
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 24;
    public const int MaxEmailLength = 254;

    public static readonly Error UsernameRequired =
        new("User.UsernameRequired", "Username is required.");

    public static readonly Error UsernameLengthOutOfRange =
        new(
            "User.UsernameLengthOutOfRange",
            $"Username must be between {MinUsernameLength} and {MaxUsernameLength} characters.");

    public static readonly Error UsernameInvalidCharacters =
        new(
            "User.UsernameInvalidCharacters",
            "Username may only contain letters, digits, underscores, and hyphens.");

    public static readonly Error EmailRequired =
        new("User.EmailRequired", "Email is required.");

    public static readonly Error EmailInvalid =
        new("User.EmailInvalid", "Email is not a valid address.");

    public static readonly Error PasswordHashRequired =
        new("User.PasswordHashRequired", "Password hash is required.");

    public static readonly Error ExternalProviderRequired =
        new("User.ExternalProviderRequired", "External provider is required.");

    public static readonly Error ExternalProviderIdRequired =
        new("User.ExternalProviderIdRequired", "External provider id is required.");

    public static readonly Error UnknownGameMode =
        new("User.UnknownGameMode", "Game mode is not a known value.");

    public static readonly Error UnknownGameOutcome =
        new("User.UnknownGameOutcome", "Game outcome is not a known value.");

    public static readonly Error GuestCannotBeRated =
        new("User.GuestCannotBeRated", "Guest accounts cannot gain rating.");

    public static readonly Error GuestCannotAccumulateStatistics =
        new("User.GuestCannotAccumulateStatistics", "Guest accounts cannot accumulate statistics.");

    private readonly Dictionary<GameMode, Glicko2Rating> _ratings;

    private User(
        string? username,
        string? email,
        string? passwordHash,
        string? externalProvider,
        string? externalProviderId,
        bool isGuest)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        ExternalProvider = externalProvider;
        ExternalProviderId = externalProviderId;
        IsGuest = isGuest;
        _ratings = Enum.GetValues<GameMode>().ToDictionary(mode => mode, _ => Glicko2Rating.Initial);
    }

    public string? Username { get; private set; }

    public string DisplayName => Username ?? $"Guest-{Id.ToString("N")[..8]}";

    public string? Email { get; private set; }

    public string? PasswordHash { get; private set; }

    public string? ExternalProvider { get; private set; }

    public string? ExternalProviderId { get; private set; }

    public bool IsGuest { get; private set; }

    public bool IsExternal => ExternalProvider is not null;

    public IReadOnlyDictionary<GameMode, Glicko2Rating> Ratings => _ratings;

    public int GamesPlayed { get; private set; }

    public int GamesWon { get; private set; }

    public int GamesDrawn { get; private set; }

    public double WinRate => GamesPlayed == 0 ? 0.0 : (double)GamesWon / GamesPlayed;

    public static Result<User> RegisterWithPassword(string username, string email, string passwordHash)
    {
        Result<string> normalizedUsername = NormalizeUsername(username);
        if (normalizedUsername.IsFailure)
        {
            return Result.Failure<User>(normalizedUsername.Error);
        }

        Result<string> normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.IsFailure)
        {
            return Result.Failure<User>(normalizedEmail.Error);
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure<User>(PasswordHashRequired);
        }

        return Result.Success(new User(
            normalizedUsername.Value,
            normalizedEmail.Value,
            passwordHash,
            externalProvider: null,
            externalProviderId: null,
            isGuest: false));
    }

    public static Result<User> RegisterWithExternalProvider(
        string username,
        string email,
        string externalProvider,
        string externalProviderId)
    {
        Result<string> normalizedUsername = NormalizeUsername(username);
        if (normalizedUsername.IsFailure)
        {
            return Result.Failure<User>(normalizedUsername.Error);
        }

        Result<string> normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.IsFailure)
        {
            return Result.Failure<User>(normalizedEmail.Error);
        }

        if (string.IsNullOrWhiteSpace(externalProvider))
        {
            return Result.Failure<User>(ExternalProviderRequired);
        }

        if (string.IsNullOrWhiteSpace(externalProviderId))
        {
            return Result.Failure<User>(ExternalProviderIdRequired);
        }

        return Result.Success(new User(
            normalizedUsername.Value,
            normalizedEmail.Value,
            passwordHash: null,
            externalProvider.Trim(),
            externalProviderId.Trim(),
            isGuest: false));
    }

    public static User CreateGuest() =>
        new(
            username: null,
            email: null,
            passwordHash: null,
            externalProvider: null,
            externalProviderId: null,
            isGuest: true);

    public Glicko2Rating GetRating(GameMode gameMode) =>
        _ratings.TryGetValue(gameMode, out Glicko2Rating? rating) ? rating : Glicko2Rating.Initial;

    public Result ApplyRating(GameMode gameMode, Glicko2Rating rating)
    {
        ArgumentNullException.ThrowIfNull(rating);

        if (IsGuest)
        {
            return Result.Failure(GuestCannotBeRated);
        }

        if (!Enum.IsDefined(gameMode))
        {
            return Result.Failure(UnknownGameMode);
        }

        _ratings[gameMode] = rating;
        Touch();

        return Result.Success();
    }

    public Result RecordGamePlayed(GameOutcome outcome)
    {
        if (IsGuest)
        {
            return Result.Failure(GuestCannotAccumulateStatistics);
        }

        if (!Enum.IsDefined(outcome))
        {
            return Result.Failure(UnknownGameOutcome);
        }

        GamesPlayed++;

        switch (outcome)
        {
            case GameOutcome.Win:
                GamesWon++;
                break;
            case GameOutcome.Draw:
                GamesDrawn++;
                break;
            case GameOutcome.Loss:
                break;
        }

        Touch();

        return Result.Success();
    }

    private static Result<string> NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure<string>(UsernameRequired);
        }

        string trimmed = username.Trim();

        if (trimmed.Length < MinUsernameLength || trimmed.Length > MaxUsernameLength)
        {
            return Result.Failure<string>(UsernameLengthOutOfRange);
        }

        foreach (char character in trimmed)
        {
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
            {
                return Result.Failure<string>(UsernameInvalidCharacters);
            }
        }

        return Result.Success(trimmed);
    }

    private static Result<string> NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<string>(EmailRequired);
        }

        string trimmed = email.Trim();

        if (trimmed.Length > MaxEmailLength)
        {
            return Result.Failure<string>(EmailInvalid);
        }

        if (trimmed.Any(char.IsWhiteSpace))
        {
            return Result.Failure<string>(EmailInvalid);
        }

        int atIndex = trimmed.IndexOf('@', StringComparison.Ordinal);

        // Exactly one '@', with a non-empty local part and a dotted domain that
        // neither starts nor ends with the separator.
        if (atIndex <= 0 || atIndex != trimmed.LastIndexOf('@'))
        {
            return Result.Failure<string>(EmailInvalid);
        }

        string domain = trimmed[(atIndex + 1)..];

        if (domain.Length < 3 || domain[0] == '.' || domain[^1] == '.'
            || !domain.Contains('.', StringComparison.Ordinal))
        {
            return Result.Failure<string>(EmailInvalid);
        }

        return Result.Success(trimmed);
    }
}
