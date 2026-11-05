namespace xml;

/// <summary>
/// Configures and retrieves bounded XML byte accumulation.
/// </summary>
public interface IXmlByteCollector
{
    /// <summary>
    /// Sets the maximum retained byte count and clears retained bytes.
    /// </summary>
    /// <param name="maximumCount">The positive maximum byte count.</param>
    void ConfigureXmlByteCollection(int maximumCount);

    /// <summary>
    /// Returns the retained bytes in arrival order.
    /// </summary>
    /// <returns>A snapshot of the retained bytes.</returns>
    byte[] SnapshotCollectedXmlBytes();

    /// <summary>
    /// Returns the retained byte count.
    /// </summary>
    /// <returns>The retained byte count.</returns>
    int CountCollectedXmlBytes();

    /// <summary>
    /// Clears retained bytes while preserving the configured maximum.
    /// </summary>
    void ClearCollectedXmlBytes();

    /// <summary>
    /// Clears retained bytes and restores the default maximum.
    /// </summary>
    void ResetXmlByteCollection();
}
