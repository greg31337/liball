namespace version;

/// <summary>
/// Defines the supported bound values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum BoundKind
{
    /// <summary>
    /// Uses unbounded behavior.
    /// </summary>
    Unbounded = 0,
    /// <summary>
    /// Uses inclusive behavior.
    /// </summary>
    Inclusive = 1,
    /// <summary>
    /// Uses exclusive behavior.
    /// </summary>
    Exclusive = 2
}
