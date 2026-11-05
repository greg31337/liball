namespace xml;

/// <summary>
/// Configures and retrieves bounded XML diagnostic accumulation.
/// </summary>
public interface IXmlDiagnosticCollector
{
    /// <summary>
    /// Sets the maximum retained diagnostic count and clears retained diagnostics.
    /// </summary>
    /// <param name="maximumCount">The positive maximum diagnostic count.</param>
    void ConfigureXmlDiagnosticCollection(int maximumCount);

    /// <summary>
    /// Returns the retained diagnostics in arrival order.
    /// </summary>
    /// <returns>A snapshot of the retained diagnostics.</returns>
    XmlDiagnostic[] SnapshotCollectedXmlDiagnostics();

    /// <summary>
    /// Returns the retained diagnostic count.
    /// </summary>
    /// <returns>The retained diagnostic count.</returns>
    int CountCollectedXmlDiagnostics();

    /// <summary>
    /// Clears retained diagnostics while preserving the configured maximum.
    /// </summary>
    void ClearCollectedXmlDiagnostics();

    /// <summary>
    /// Clears retained diagnostics and restores the default maximum.
    /// </summary>
    void ResetXmlDiagnosticCollection();
}
