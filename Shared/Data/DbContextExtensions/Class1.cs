using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Usm.Shared.Contracts.Localization;

namespace Usm.Shared.Data.DbContextExtensions;

public interface IMultiTenant
{
    Guid TenantId { get; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; }
}

public static class ModelBuilderExtensions
{
    public static PropertyBuilder<LocalizedText> HasJsonbLocalization(this PropertyBuilder<LocalizedText> builder)
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        builder.HasConversion(
            value => JsonSerializer.Serialize(value, serializerOptions),
            value => JsonSerializer.Deserialize<LocalizedText>(value, serializerOptions) ?? LocalizedText.Empty);
        builder.HasColumnType("jsonb");
        return builder;
    }

    public static void ApplySoftDeleteFilter<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeletable
    {
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }

    public static void ApplyTenantFilter<TEntity>(this EntityTypeBuilder<TEntity> builder, Guid tenantId)
        where TEntity : class, IMultiTenant
    {
        builder.HasQueryFilter(entity => entity.TenantId == tenantId);
    }
}

public static class DbContextQueryExtensions
{
    public static IQueryable<TEntity> ExcludingDeleted<TEntity>(this IQueryable<TEntity> source)
        where TEntity : class, ISoftDeletable
    {
        return source.Where(entity => !entity.IsDeleted);
    }

    public static IQueryable<TEntity> ForTenant<TEntity>(this IQueryable<TEntity> source, Guid tenantId)
        where TEntity : class, IMultiTenant
    {
        return source.Where(entity => entity.TenantId == tenantId);
    }

    public static Task<bool> ExistsDuplicateAsync<TEntity>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return dbSet.AnyAsync(predicate, cancellationToken);
    }
}
