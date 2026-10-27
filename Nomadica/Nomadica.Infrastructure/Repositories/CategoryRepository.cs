using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public class CategoryRepository : BaseRepository<Category, CategoryId>, ICategoryRepository
{
    public CategoryRepository(NomadiaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Context.Categories.OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);
}
