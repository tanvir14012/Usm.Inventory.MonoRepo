using Usm.Shared.Algorithms.Compression.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Compression.Tests;

public sealed class CompressionAlgorithmsTests
{
    [Fact]
    public void RunLengthEncodingWorks()
    {
        var alg = CompressionAlgorithmsExtensions.CreateBuilder().Build();
        var encoded = alg.RunLengthEncode("aaabbbccc");
        var decoded = alg.RunLengthDecode(encoded);
        Assert.Equal("aaabbbccc", decoded);
    }

    [Fact]
    public void DeltaEncodingWorks()
    {
        var alg = CompressionAlgorithmsExtensions.CreateBuilder().Build();
        var bytes = new byte[] { 10, 15, 13, 20 };
        var encoded = alg.DeltaEncode(bytes);
        var decoded = alg.DeltaDecode(encoded);
        Assert.Equal(bytes, decoded);
    }

    [Fact]
    public void HuffmanEncodingWorks()
    {
        var alg = CompressionAlgorithmsExtensions.CreateBuilder().Build();
        var (encoded, map) = alg.HuffmanEncode("abcaab");
        var decoded = alg.HuffmanDecode(encoded, map);
        Assert.Equal("abcaab", decoded);
    }
}
