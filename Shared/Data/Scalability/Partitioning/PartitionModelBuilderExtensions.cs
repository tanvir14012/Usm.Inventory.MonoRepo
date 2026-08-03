using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Usm.Shared.Data.Scalability.Partitioning;

/// <summary>Well-known EF Core annotation keys written by the partition fluent API.</summary>
public static class PartitionAnnotations
{
    public const string PartitionType = "Scalability:PartitionType";
    public const string PartitionColumns = "Scalability:PartitionColumns";
    public const string HashModulus = "Scalability:HashModulus";
    public const string RangeInterval = "Scalability:RangeInterval";
}

/// <summary>
/// EF Core model-builder extensions for PostgreSQL table partitioning.
/// <para>
/// These methods record partition metadata as EF Core annotations.
/// The actual <c>PARTITION BY …</c> DDL is produced by
/// <see cref="GenerateCreatePartitionedTableDdl{TEntity}"/>, which should be placed
/// in your <c>ScriptMigrationEngine</c> seed directory or an EF Core migration.
/// </para>
/// </summary>
public static class PartitionModelBuilderExtensions
{
    /// <summary>
    /// Configures RANGE partitioning driven by a single column expression.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasRangePartition<TEntity, TKey>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TKey>> partitionColumn,
        TimeSpan interval)
        where TEntity : class
    {
        var column = GetColumnName(partitionColumn);
        builder.HasAnnotation(PartitionAnnotations.PartitionType, PartitionType.Range.ToString());
        builder.HasAnnotation(PartitionAnnotations.PartitionColumns, column);
        builder.HasAnnotation(PartitionAnnotations.RangeInterval, interval.ToString());
        return builder;
    }

    /// <summary>Configures LIST partitioning driven by a single column expression.</summary>
    public static EntityTypeBuilder<TEntity> HasListPartition<TEntity, TKey>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TKey>> partitionColumn)
        where TEntity : class
    {
        var column = GetColumnName(partitionColumn);
        builder.HasAnnotation(PartitionAnnotations.PartitionType, PartitionType.List.ToString());
        builder.HasAnnotation(PartitionAnnotations.PartitionColumns, column);
        return builder;
    }

    /// <summary>Configures HASH partitioning with a given modulus (number of child partitions).</summary>
    public static EntityTypeBuilder<TEntity> HasHashPartition<TEntity, TKey>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TKey>> partitionColumn,
        int modulus = 8)
        where TEntity : class
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(modulus, 2);
        var column = GetColumnName(partitionColumn);
        builder.HasAnnotation(PartitionAnnotations.PartitionType, PartitionType.Hash.ToString());
        builder.HasAnnotation(PartitionAnnotations.PartitionColumns, column);
        builder.HasAnnotation(PartitionAnnotations.HashModulus, modulus);
        return builder;
    }

    /// <summary>
    /// Generates idempotent DDL for a PostgreSQL partitioned parent table based on the EF Core
    /// annotations added by <see cref="HasRangePartition{TEntity,TKey}"/>,
    /// <see cref="HasListPartition{TEntity,TKey}"/>, or
    /// <see cref="HasHashPartition{TEntity,TKey}"/>.
    /// <para>
    /// Use the generated SQL inside a seed script or EF Core migration to create the parent table.
    /// </para>
    /// </summary>
    public static string GenerateCreatePartitionedTableDdl<TEntity>(
        ModelBuilder modelBuilder,
        string schema = "public")
        where TEntity : class
    {
        var entityType = modelBuilder.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).Name}' is not registered in the model.");

        var table = entityType.GetTableName() ?? typeof(TEntity).Name.ToLowerInvariant();
        var typeStr = entityType.FindAnnotation(PartitionAnnotations.PartitionType)?.Value?.ToString()
            ?? throw new InvalidOperationException($"No partition type annotation on '{typeof(TEntity).Name}'.");
        var columns = entityType.FindAnnotation(PartitionAnnotations.PartitionColumns)?.Value?.ToString()
            ?? throw new InvalidOperationException($"No partition columns annotation on '{typeof(TEntity).Name}'.");

        var sb = new StringBuilder();
        sb.AppendLine($"-- Auto-generated partitioned-table DDL for {typeof(TEntity).Name}");
        sb.AppendLine($"-- Execute once; the child partition tables are created separately.");
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {schema}.{table}_partitioned (");
        sb.AppendLine($"    LIKE {schema}.{table} INCLUDING ALL");
        sb.AppendLine($") PARTITION BY {typeStr.ToUpperInvariant()} ({columns});");

        // For HASH partitions, scaffold all child table stubs.
        if (typeStr.Equals(PartitionType.Hash.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var modulus = (int?)entityType.FindAnnotation(PartitionAnnotations.HashModulus)?.Value ?? 8;
            for (var i = 0; i < modulus; i++)
            {
                sb.AppendLine($"CREATE TABLE IF NOT EXISTS {schema}.{table}_p{i:D2}");
                sb.AppendLine($"    PARTITION OF {schema}.{table}_partitioned");
                sb.AppendLine($"    FOR VALUES WITH (MODULUS {modulus}, REMAINDER {i});");
            }
        }

        return sb.ToString();
    }

    private static string GetColumnName<TEntity, TKey>(Expression<Func<TEntity, TKey>> expression)
        where TEntity : class
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException(
            "Partition key expression must be a simple property access (e.g. x => x.CreatedAt).",
            nameof(expression));
    }
}
