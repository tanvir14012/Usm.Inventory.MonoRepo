namespace Usm.Shared.Data.Scalability.Replication;

/// <summary>
/// Ambient context signalling whether the current async execution scope should
/// route database reads to a read replica.
/// </summary>
public static class ReadReplicaContext
{
    private static readonly AsyncLocal<bool> _isReadMode = new();
    private static readonly AsyncLocal<bool> _forcePrimary = new();

    /// <summary>
    /// <c>true</c> when read-replica routing is active and not overridden by
    /// <see cref="ForceDisableForCurrentScope"/> (e.g., due to a recent write).
    /// </summary>
    public static bool IsReadMode => _isReadMode.Value && !_forcePrimary.Value;

    /// <summary>
    /// Opens a scope in which all EF Core SELECT commands are redirected to the replica.
    /// The original flag is restored when the returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <example>
    /// <code>
    /// using (ReadReplicaContext.UseReadReplica())
    /// {
    ///     var result = await dbContext.Products.ToListAsync();
    /// }
    /// </code>
    /// </example>
    public static IDisposable UseReadReplica()
    {
        var previous = _isReadMode.Value;
        _isReadMode.Value = true;
        return new RestoreScope(() => _isReadMode.Value = previous);
    }

    /// <summary>
    /// Forces the current async context to use the primary for the next read —
    /// called by <c>EventualConsistencyStrategy</c> after detecting a recent write.
    /// </summary>
    public static void ForceDisableForCurrentScope() => _forcePrimary.Value = true;

    /// <summary>Clears the forced-primary override (call at the start of a new logical operation).</summary>
    public static void ResetForcePrimary() => _forcePrimary.Value = false;

    private sealed class RestoreScope(Action restore) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
                return;
            restore();
            _disposed = true;
        }
    }
}
