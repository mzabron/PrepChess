namespace PrepChess.Domain.Common;

public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected BaseEntity()
        : this(Guid.NewGuid())
    {
    }

    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; protected set; }

    public DateTime UpdatedAt { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Touch() => UpdatedAt = DateTime.UtcNow;

    public override bool Equals(object? obj) =>
        obj is BaseEntity other
        && other.GetType() == GetType()
        && other.Id == Id
        && Id != Guid.Empty;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
