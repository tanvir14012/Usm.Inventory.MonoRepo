using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Usm.Shared.Utils.Excel.Writer.Internal;

/// <summary>
/// Thread-safe, process-lifetime cache of compiled property setter delegates.
/// Uses <see cref="PropertyInfo.MetadataToken"/> to guarantee declaration order,
/// which drives the column→property positional mapping.
/// </summary>
internal static class PropertyAccessorCache
{
    private static readonly ConcurrentDictionary<Type, PropertyDescriptor[]> Cache = new();

    internal static PropertyDescriptor[] GetDescriptors(Type type)
        => Cache.GetOrAdd(type, BuildDescriptors);

    private static PropertyDescriptor[] BuildDescriptors(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .OrderBy(p => p.MetadataToken)   // reliable declaration order within one type
            .Select((p, columnIndex) => new PropertyDescriptor(
                ColumnIndex: columnIndex,
                Property: p,
                Setter: CompileSetter(type, p),
                PropertyType: p.PropertyType,
                IsNullable: !p.PropertyType.IsValueType ||
                             Nullable.GetUnderlyingType(p.PropertyType) is not null,
                UnderlyingType: Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType))
            .ToArray();
    }

    /// <summary>
    /// Compiles a lambda: <c>(object instance, object? value) => ((TOwner)instance).Prop = (TProp)value</c>
    /// Avoids reflection overhead on every call after the first.
    /// </summary>
    private static Action<object, object?> CompileSetter(Type ownerType, PropertyInfo prop)
    {
        var instance     = Expression.Parameter(typeof(object), "instance");
        var value        = Expression.Parameter(typeof(object), "value");
        var castInstance = Expression.Convert(instance, ownerType);
        var castValue    = Expression.Convert(value, prop.PropertyType);
        var body         = Expression.Assign(Expression.Property(castInstance, prop), castValue);
        return Expression.Lambda<Action<object, object?>>(body, instance, value).Compile();
    }
}

internal sealed record PropertyDescriptor(
    int ColumnIndex,
    PropertyInfo Property,
    Action<object, object?> Setter,
    Type PropertyType,
    bool IsNullable,
    Type UnderlyingType);
