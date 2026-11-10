using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public class CategoryRepository : BaseRepository<Category, CategoryId>, ICategoryRepository
{
    public CategoryRepository(BearNecessitiesDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Context.Categories.OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);
}
