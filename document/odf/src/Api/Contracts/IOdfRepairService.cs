namespace odf;

/// <summary>
/// Analyzes and explicitly applies safe repairs to OpenDocument documents.
/// </summary>
public interface IOdfRepairService
{
    /// <summary>
    /// Produces repair proposals without mutating the document.
    /// </summary>
    /// <param name="document">The document to analyze.</param>
    /// <returns>The safe and unsafe repair proposals.</returns>
    public OdfRepairAnalysis ProposeRepairs(OdfDocument document);
    /// <summary>
    /// Applies selected safe proposals when their analyzed state is still current.
    /// </summary>
    /// <param name="document">The document to repair.</param>
    /// <param name="proposals">Original proposals returned for the document.</param>
    /// <returns>The application result and mutation report.</returns>
    public OdfRepairResult ApplyRepairs(
        OdfDocument document,
        OdfRepairProposal[] proposals);
}
