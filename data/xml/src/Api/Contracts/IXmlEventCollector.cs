namespace xml;

/// <summary>
/// Configures and retrieves bounded normalized XML event accumulation.
/// </summary>
public interface IXmlEventCollector
{
    /// <summary>
    /// Sets the maximum retained event count and clears retained events.
    /// </summary>
    /// <param name="maximumCount">The positive maximum event count.</param>
    void ConfigureXmlEventCollection(int maximumCount);

    /// <summary>
    /// Returns the retained events in arrival order.
    /// </summary>
    /// <returns>A snapshot of the retained events.</returns>
    XmlEvent[] SnapshotCollectedXmlEvents();

    /// <summary>
    /// Returns the retained event count.
    /// </summary>
    /// <returns>The retained event count.</returns>
    int CountCollectedXmlEvents();

    /// <summary>
    /// Clears retained events while preserving the configured maximum.
    /// </summary>
    void ClearCollectedXmlEvents();

    /// <summary>
    /// Clears retained events and restores the default maximum.
    /// </summary>
    void ResetXmlEventCollection();
}
