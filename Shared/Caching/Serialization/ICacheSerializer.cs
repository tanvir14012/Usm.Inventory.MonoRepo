namespace Usm.Shared.Caching.Serialization;

public interface ICacheSerializer
{
    byte[] Serialize<T>(T value);
    T? Deserialize<T>(byte[] bytes);
}
