namespace icc;

/// <summary>
/// Identifies a byte range in a profile or containing data source.
/// </summary>
public class IccByteLocation
{
    /// <summary>
    /// Gets or sets the zero-based byte offset, or <c>-1</c> when no exact offset is known.
    /// </summary>
    public long Offset;

    /// <summary>
    /// Gets or sets the byte count associated with the location.
    /// </summary>
    public long Length;

    /// <summary>
    /// Initializes an unknown, empty byte location.
    /// </summary>
    public IccByteLocation()
    {
        Offset = -1;
        Length = 0;
    }
}
