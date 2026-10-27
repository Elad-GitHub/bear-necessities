using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public abstract class BaseRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>
{
    protected readonly NomadiaDbContext Context;

    protected BaseRepository(NomadiaDbContext context)
    {
        Context = context;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => await Context.Set<TEntity>().FindAsync(new object?[] { id }, cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await Context.Set<TEntity>().AddAsync(entity, cancellationToken);

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Set<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Set<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }
}
