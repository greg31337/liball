namespace unicode;

/// <summary>
/// Reports the outcome of writing encoded, decoded, or scalar data into a caller-provided buffer.
/// </summary>
public class UnicodeBufferResult
{
    /// <summary>
    /// Whether the complete result was written to the destination.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The destination length required to hold the complete result.
    /// </summary>
    public int RequiredLength;
    /// <summary>
    /// The number of destination elements written.
    /// </summary>
    public int WrittenLength;

    /// <summary>
    /// Initializes a new buffer result.
    /// </summary>
    /// <param name="success">Whether the complete result was written.</param>
    /// <param name="requiredLength">The required destination length.</param>
    /// <param name="writtenLength">The number of destination elements written.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a length is negative or inconsistent with <c>success</c>.</exception>
    public UnicodeBufferResult(bool success, int requiredLength, int writtenLength)
    {
        if (requiredLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("requiredLength");
        }

        if (writtenLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("writtenLength");
        }

        if (writtenLength > requiredLength)
        {
            throw new System.ArgumentOutOfRangeException("writtenLength");
        }

        if (success && writtenLength != requiredLength)
        {
            throw new System.ArgumentException("A successful result must write the complete required length.");
        }

        Success = success;
        RequiredLength = requiredLength;
        WrittenLength = writtenLength;
    }
}
