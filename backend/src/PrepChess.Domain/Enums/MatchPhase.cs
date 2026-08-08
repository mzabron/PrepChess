namespace PrepChess.Domain.Enums;

public enum MatchPhase
{
    WaitingForOpponent = 0,

    DeckSelection = 1,

    Banning = 2,

    Picking = 3,

    Drafting = 4,

    InProgress = 5,

    Completed = 6,
}
