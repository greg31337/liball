namespace asn1;

/// <summary>
/// Contains exact input and output progress for a stream operation.
/// </summary>
public class AsnStreamProgress
{
    /// <summary>
    /// Contains the number of input units consumed.
    /// </summary>
    public int ConsumedCount;

    /// <summary>
    /// Contains the number of output units written.
    /// </summary>
    public int WrittenCount;

    /// <summary>
    /// Indicates whether ExpectedCount is known.
    /// </summary>
    public bool HasExpectedCount;

    /// <summary>
    /// Contains the expected total input count when HasExpectedCount is true.
    /// </summary>
    public int ExpectedCount;

    /// <summary>
    /// Indicates whether the operation completed.
    /// </summary>
    public bool Complete;
}
