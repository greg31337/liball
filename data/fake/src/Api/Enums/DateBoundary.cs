namespace fake;

/// <summary>
/// Selects whether the maximum of a bounded temporal interval is included.
/// </summary>
public enum DateBoundary
{
    /// <summary>
    /// The maximum bound is excluded and the interval must contain at least one representable value below it.
    /// </summary>
    Exclusive = 0,

    /// <summary>
    /// The maximum bound is included and may equal the minimum.
    /// </summary>
    Inclusive = 1
}
