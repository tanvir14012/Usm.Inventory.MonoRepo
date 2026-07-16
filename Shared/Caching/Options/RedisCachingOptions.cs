namespace Usm.Shared.Caching.Options;

public sealed class RedisCachingOptions
{
    public const string SectionName = "Caching:Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "usm-cache";
    public string KeyPrefix { get; set; } = "usm";
    public int DefaultAbsoluteExpirationSeconds { get; set; } = 300;
    public int? DefaultSlidingExpirationSeconds { get; set; }
    public bool EnableCompression { get; set; } = true;
    public int CompressionThresholdBytes { get; set; } = 1024;
    public int ConnectRetry { get; set; } = 3;
    public int ConnectTimeoutMilliseconds { get; set; } = 5000;
    public int SyncTimeoutMilliseconds { get; set; } = 5000;
    public bool UseCamelCaseJson { get; set; } = true;
}
