using System.Linq.Expressions;

namespace Usm.Shared.Data.Scalability.Abstractions;

/// <summary>Encapsulates partition-aware query routing for a specific entity type.</summary>
public interface IPartitionStrategy<TEntity> where TEntity : class
{
    /// <summary>Resolves the child partition table name for a given entity (e.g., "orders_2024_01").</summary>
    string ResolvePartitionName(TEntity entity);

    /// <summary>
    /// Returns a predicate that constrains the query to the target partition.
    /// PostgreSQL's planner prunes other partitions automatically when this predicate is included.
    /// </summary>
    Expression<Func<TEntity, bool>> GetPartitionPredicate(object partitionKey);
}
