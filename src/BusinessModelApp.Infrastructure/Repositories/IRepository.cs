using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public interface IRepository<TEntity, TKey> where TEntity : IEntity<TKey> where TKey : IEquatable<TKey>
    {
        IQueryable<TEntity> GetAll();
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken cancellationToken = default);
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    // For backward compatibility with existing repositories using Guid
    public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : IEntity<Guid>
    {
    }
}
