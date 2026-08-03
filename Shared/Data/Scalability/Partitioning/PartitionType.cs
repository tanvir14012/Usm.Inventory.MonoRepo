namespace Usm.Shared.Data.Scalability.Partitioning;

public enum PartitionType
{
    /// <summary>Partitions by a contiguous range of values (e.g., monthly date ranges).</summary>
    Range,

    /// <summary>Partitions by a discrete set of values (e.g., region codes, status enums).</summary>
    List,

    /// <summary>
    /// Partitions by hash modulo — used when no natural range or list boundary exists
    /// and uniform distribution is desired.
    /// </summary>
    Hash,
}
