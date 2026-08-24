using PrepChess.Domain.Common;

namespace PrepChess.Domain.Events;

/// <summary>
/// Raised when a user unlocks a new opening card through the progression system.
/// </summary>
public sealed record CardUnlockedDomainEvent(Guid UserId, Guid OpeningCardId) : IDomainEvent;
