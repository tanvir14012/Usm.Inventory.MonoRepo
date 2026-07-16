namespace Usm.Shared.Caching.Keys;

public interface ICacheKeyGenerator
{
    string Build(params string?[] segments);
}
