using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class GroupPost : BaseEntity<Guid>
{
    public const int MaxBodyLength = 4000;

    private GroupPost() { }

    public GroupId GroupId { get; private set; } = default!;
    public UserId AuthorId { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public bool IsRemoved { get; private set; }

    public static GroupPost Create(GroupId groupId, UserId authorId, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Post body cannot be empty.", nameof(body));
        if (body.Length > MaxBodyLength)
            throw new ArgumentException($"Post body cannot exceed {MaxBodyLength} characters.", nameof(body));

        return new GroupPost
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            AuthorId = authorId,
            Body = body.Trim()
        };
    }

    public void Remove()
    {
        IsRemoved = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
