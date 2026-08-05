namespace Usm.Shared.Patterns.Factory.Abstractions;

/// <summary>
/// Compiles expression-backed factories and caches them when configured.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TProduct">The produced type.</typeparam>
public interface IFactoryCompiler<TContext, TProduct>
{
    /// <summary>Compiles the supplied factory to a delegate.</summary>
    Func<TContext, TProduct> Compile(IFactory<TContext, TProduct> factory);
}
