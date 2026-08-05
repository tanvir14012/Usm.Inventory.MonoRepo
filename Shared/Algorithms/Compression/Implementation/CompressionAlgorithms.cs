using System.Text;
using Usm.Shared.Algorithms.Compression.Abstractions;

namespace Usm.Shared.Algorithms.Compression.Implementation;

/// <summary>
/// Compression algorithms.
/// </summary>
public sealed class CompressionAlgorithms : ICompressionAlgorithms
{
    /// <inheritdoc />
    public string RunLengthEncode(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        var count = 1;

        for (var i = 1; i < input.Length; i++)
        {
            if (input[i] == input[i - 1] && count < 255)
            {
                count++;
            }
            else
            {
                sb.Append(count);
                sb.Append(input[i - 1]);
                count = 1;
            }
        }

        sb.Append(count);
        sb.Append(input[^1]);
        return sb.ToString();
    }

    /// <inheritdoc />
    public string RunLengthDecode(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        for (var i = 0; i < input.Length; i += 2)
        {
            if (int.TryParse(input[i].ToString(), out var count) && i + 1 < input.Length)
            {
                sb.Append(input[i + 1], count);
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public (string, Dictionary<char, string>) HuffmanEncode(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Length == 0)
            return (string.Empty, new Dictionary<char, string>());

        var freq = new Dictionary<char, int>();
        foreach (var c in input)
        {
            if (!freq.ContainsKey(c))
                freq[c] = 0;
            freq[c]++;
        }

        var heap = new PriorityQueue<HuffmanNode, int>();
        foreach (var (c, f) in freq)
            heap.Enqueue(new HuffmanNode { Char = c, Freq = f }, f);

        while (heap.Count > 1)
        {
            var left = heap.Dequeue();
            var right = heap.Dequeue();
            var parent = new HuffmanNode { Freq = left.Freq + right.Freq, Left = left, Right = right };
            heap.Enqueue(parent, parent.Freq);
        }

        var root = heap.Dequeue();
        var map = new Dictionary<char, string>();
        BuildHuffmanMap(root, "", map);

        var encoded = new StringBuilder();
        foreach (var c in input)
            encoded.Append(map[c]);

        return (encoded.ToString(), map);
    }

    /// <inheritdoc />
    public string HuffmanDecode(string encoded, Dictionary<char, string> map)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        ArgumentNullException.ThrowIfNull(map);

        var reverseMap = new Dictionary<string, char>();
        foreach (var (c, code) in map)
            reverseMap[code] = c;

        var sb = new StringBuilder();
        var current = new StringBuilder();

        foreach (var bit in encoded)
        {
            current.Append(bit);
            if (reverseMap.TryGetValue(current.ToString(), out var c))
            {
                sb.Append(c);
                current.Clear();
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public byte[] DeltaEncode(byte[] input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Length == 0)
            return [];

        var output = new byte[input.Length];
        output[0] = input[0];

        for (var i = 1; i < input.Length; i++)
            output[i] = unchecked((byte)(input[i] - input[i - 1]));

        return output;
    }

    /// <inheritdoc />
    public byte[] DeltaDecode(byte[] input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Length == 0)
            return [];

        var output = new byte[input.Length];
        output[0] = input[0];

        for (var i = 1; i < input.Length; i++)
            output[i] = unchecked((byte)(output[i - 1] + input[i]));

        return output;
    }

    private sealed class HuffmanNode
    {
        public char Char { get; set; }
        public int Freq { get; set; }
        public HuffmanNode? Left { get; set; }
        public HuffmanNode? Right { get; set; }
    }

    private static void BuildHuffmanMap(HuffmanNode node, string code, Dictionary<char, string> map)
    {
        if (node.Left == null && node.Right == null)
        {
            map[node.Char] = code.Length > 0 ? code : "0";
            return;
        }

        if (node.Left != null)
            BuildHuffmanMap(node.Left, code + "0", map);
        if (node.Right != null)
            BuildHuffmanMap(node.Right, code + "1", map);
    }
}
