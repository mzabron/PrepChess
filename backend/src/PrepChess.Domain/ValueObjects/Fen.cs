using Chess;
using PrepChess.Domain.Common;

namespace PrepChess.Domain.ValueObjects;

public sealed record Fen
{
    public const string StartingPositionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public static readonly Error Empty = new("Fen.Empty", "A FEN string is required.");

    public static readonly Error Invalid = new("Fen.Invalid", "The value is not a valid FEN string.");

    private Fen(string value) => Value = value;

    public string Value { get; }

    public static Fen StartingPosition { get; } = new(StartingPositionFen);

    public Enums.PieceColor SideToMove =>
        Value.Split(' ')[1].Equals("b", StringComparison.OrdinalIgnoreCase)
            ? Enums.PieceColor.Black
            : Enums.PieceColor.White;

    public static Result<Fen> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Fen>(Empty);
        }

        string trimmed = value.Trim();

        return ChessBoard.TryLoadFromFen(trimmed, out _)
            ? Result.Success(new Fen(trimmed))
            : Result.Failure<Fen>(Invalid);
    }

    public override string ToString() => Value;
}
