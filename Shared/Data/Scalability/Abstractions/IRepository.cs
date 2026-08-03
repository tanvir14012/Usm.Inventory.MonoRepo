namespace Usm.Shared.Data.Scalability.Abstractions;

/// <summary>
/// Generic repository abstraction. Implementations wrap an EF Core DbContext
/// and may route reads through <c>IDatabaseScalingStrategy&lt;TEntity&gt;</c>.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Returns a queryable source. Pass <paramref name="readOnly"/> = <c>true</c> to enable read-replica routing.</summary>
    IQueryable<TEntity> Query(bool readOnly = false);

    ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);

    ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);

    ValueTask<int> SaveAsync(CancellationToken cancellationToken = default);
}
