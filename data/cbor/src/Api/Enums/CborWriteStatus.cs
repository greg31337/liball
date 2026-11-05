namespace cbor;

/// <summary>
/// Describes the outcome of a CBOR write operation.
/// </summary>
public enum CborWriteStatus
{
    /// <summary>No write outcome is available.</summary>
    None = 0,
    /// <summary>The requested value or token was written.</summary>
    Done = 1,
    /// <summary>The destination needs more space.</summary>
    DestinationTooSmall = 2,
    /// <summary>The value or writer state is invalid.</summary>
    Error = 3
}
