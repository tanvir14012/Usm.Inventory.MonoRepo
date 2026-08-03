using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Usm.Shared.Data.Scalability.Functions;

/// <summary>
/// EF Core model-builder extensions for registering PostgreSQL DB functions.
/// </summary>
public static class PostgreSqlFunctionModelBuilderExtensions
{
    /// <summary>
    /// Registers every public static method on <see cref="PostgreSqlDbFunctions"/> decorated with
    /// <c>[DbFunction]</c> into the EF Core model, making them available inside LINQ queries.
    /// Call once in your <c>DbContext.OnModelCreating</c>.
    /// </summary>
    public static ModelBuilder RegisterPostgreSqlBuiltInFunctions(this ModelBuilder modelBuilder)
    {
        var methods = typeof(PostgreSqlDbFunctions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(static m => m.GetCustomAttribute<DbFunctionAttribute>() is not null);

        foreach (var method in methods)
            modelBuilder.HasDbFunction(method);

        return modelBuilder;
    }

    /// <summary>
    /// Registers a custom PostgreSQL function (e.g., one deployed by the script seeding engine)
    /// so it can be called inside LINQ queries via a static stub method.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    /// <param name="methodStub">A static method that acts as the LINQ expression tree stub.</param>
    /// <param name="schema">Schema owning the function (null → <c>public</c>).</param>
    /// <param name="functionName">Override the database function name (null → uses method name).</param>
    public static ModelBuilder RegisterCustomFunction(
        this ModelBuilder modelBuilder,
        MethodInfo methodStub,
        string? schema = null,
        string? functionName = null)
    {
        var builder = modelBuilder.HasDbFunction(methodStub);

        if (!string.IsNullOrWhiteSpace(schema))
            builder.HasSchema(schema);

        if (!string.IsNullOrWhiteSpace(functionName))
            builder.HasName(functionName);

        return modelBuilder;
    }

    /// <summary>
    /// Registers a custom scalar function using a strongly-typed delegate stub.
    /// <para>
    /// The stub method must be a <c>static</c> method; the delegate is used only to resolve
    /// the <see cref="MethodInfo"/> at design time.
    /// </para>
    /// </summary>
    public static DbFunctionBuilder RegisterCustomFunction<TDelegate>(
        this ModelBuilder modelBuilder,
        TDelegate stub,
        string? schema = null,
        string? functionName = null)
        where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(stub);
        if (!stub.Method.IsStatic)
            throw new ArgumentException(
                "The delegate stub must point to a static method.", nameof(stub));

        var builder = modelBuilder.HasDbFunction(stub.Method);

        if (!string.IsNullOrWhiteSpace(schema))
            builder.HasSchema(schema);

        if (!string.IsNullOrWhiteSpace(functionName))
            builder.HasName(functionName);

        return builder;
    }

    /// <summary>
    /// Registers a stored-procedure mapping (PostgreSQL PROCEDURE) so EF Core can
    /// generate the correct CALL statement.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="methodStub">Static stub method (return type must be <c>void</c> or <c>int</c>).</param>
    /// <param name="schema">Schema owning the procedure.</param>
    /// <param name="procedureName">Override procedure name (null → method name).</param>
    public static DbFunctionBuilder RegisterCustomProcedure(
        this ModelBuilder modelBuilder,
        MethodInfo methodStub,
        string? schema = null,
        string? procedureName = null)
    {
        var builder = modelBuilder.HasDbFunction(methodStub);

        if (!string.IsNullOrWhiteSpace(schema))
            builder.HasSchema(schema);

        if (!string.IsNullOrWhiteSpace(procedureName))
            builder.HasName(procedureName);

        return builder;
    }
}
