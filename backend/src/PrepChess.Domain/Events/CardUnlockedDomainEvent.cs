using PrepChess.Domain.Common;

namespace PrepChess.Domain.Events;

public sealed record CardUnlockedDomainEvent(Guid UserId, Guid OpeningCardId) : IDomainEvent;
