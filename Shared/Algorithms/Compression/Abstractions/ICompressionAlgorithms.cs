namespace Usm.Shared.Algorithms.Compression.Abstractions;

/// <summary>
/// Represents compression algorithms.
/// </summary>
public interface ICompressionAlgorithms
{
    /// <summary>Encodes using run-length encoding.</summary>
    string RunLengthEncode(string input);

    /// <summary>Decodes run-length encoding.</summary>
    string RunLengthDecode(string input);

    /// <summary>Encodes using Huffman coding.</summary>
    (string Encoding, Dictionary<char, string> Map) HuffmanEncode(string input);

    /// <summary>Decodes Huffman coding.</summary>
    string HuffmanDecode(string encoded, Dictionary<char, string> map);

    /// <summary>Delta encodes bytes.</summary>
    byte[] DeltaEncode(byte[] input);

    /// <summary>Delta decodes bytes.</summary>
    byte[] DeltaDecode(byte[] input);
}
