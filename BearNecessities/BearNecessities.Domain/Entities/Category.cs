using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class Category : BaseEntity<CategoryId>
{
    private Category() { }

    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public CategoryId? ParentId { get; private set; }
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }

    public static Category Create(string slug, string name, int sortOrder = 0, CategoryId? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.", nameof(slug));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return new Category
        {
            Id = new CategoryId(Guid.NewGuid()),
            Slug = slug.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            SortOrder = sortOrder,
            ParentId = parentId
        };
    }
}
