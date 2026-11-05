namespace email;
/// <summary>
/// Receives progress updates from a synchronous email operation.
/// </summary>
/// <remarks>
/// Implementations may aggregate, publish, or inspect updates. Operations never require progress implementations to expose mutable counters.
/// </remarks>
public interface IEmailProgress
{
    /// <summary>
    /// Adds source bytes read by the operation.
    /// </summary>
    /// <param name = "value">The non-negative number of bytes to add.</param>
    void AddSourceBytesRead(long value);
    /// <summary>
    /// Adds output bytes written by the operation.
    /// </summary>
    /// <param name = "value">The non-negative number of bytes to add.</param>
    void AddOutputBytesWritten(long value);
    /// <summary>
    /// Adds decoded bytes produced by the operation.
    /// </summary>
    /// <param name = "value">The non-negative number of bytes to add.</param>
    void AddDecodedBytesProduced(long value);
    /// <summary>
    /// Adds headers processed by the operation.
    /// </summary>
    /// <param name = "value">The non-negative number of headers to add.</param>
    void AddHeaderCount(long value);
    /// <summary>
    /// Adds entities processed by the operation.
    /// </summary>
    /// <param name = "value">The non-negative number of entities to add.</param>
    void AddEntityCount(long value);
    /// <summary>
    /// Adds work operations completed by the operation.
    /// </summary>
    /// <param name = "value">The non-negative number of operations to add.</param>
    void AddOperationCount(long value);
}
