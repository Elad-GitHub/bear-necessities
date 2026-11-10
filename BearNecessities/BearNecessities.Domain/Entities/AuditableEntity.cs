namespace BearNecessities.Domain.Entities;

public abstract class AuditableEntity<TId> : BaseEntity<TId>
{
    public DateTime? DeletedAt { get; protected set; }
    public bool IsDeleted => DeletedAt.HasValue;

    protected void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
