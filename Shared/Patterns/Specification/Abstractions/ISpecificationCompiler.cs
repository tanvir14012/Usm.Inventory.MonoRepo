namespace Usm.Shared.Patterns.Specification.Abstractions;

/// <summary>
/// Compiles specifications and optionally caches expression-based delegates.
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public interface ISpecificationCompiler<T>
{
    /// <summary>Compiles the supplied specification to a delegate.</summary>
    Func<T, bool> Compile(ISpecification<T> specification);
}
