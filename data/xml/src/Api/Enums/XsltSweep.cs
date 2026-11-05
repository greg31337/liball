namespace xml;

/// <summary>
/// Specifies how an XSLT construct consumes its streamed input.
/// </summary>
public enum XsltSweep
{
    /// <summary>
    /// Indicates that streamed descendants are not consumed.
    /// </summary>
    Motionless = 0,
    /// <summary>
    /// Indicates one consuming traversal of streamed descendants.
    /// </summary>
    Consuming = 1,
    /// <summary>
    /// Indicates input access that is not constrained to a single consuming traversal.
    /// </summary>
    FreeRanging = 2
}
