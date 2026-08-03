namespace Usm.Shared.Data.Scalability.ScriptSeeding;

/// <summary>
/// Immutable descriptor for a discovered .sql script file.
/// <list type="table">
/// <item><term>Name</term><description>Stable unique id — path relative to the root script directory (forward slashes).</description></item>
/// <item><term>FilePath</term><description>Absolute path on disk.</description></item>
/// <item><term>Content</term><description>Full SQL text of the script.</description></item>
/// <item><term>Checksum</term><description>SHA-256 hex digest of <c>Content</c> used to detect content changes.</description></item>
/// <item><term>Order</term><description>Computed execution order: (folder precedence × 10 000) + numeric filename prefix.</description></item>
/// </list>
/// </summary>
public sealed record ScriptRecord(
    string Name,
    string FilePath,
    string Content,
    string Checksum,
    int Order);
