namespace molecule;


/// <summary>
/// Receives immutable-by-convention count snapshots during batch conversion.
/// </summary>
public interface IBatchConversionProgress
{
    /// <summary>
    /// Receives a new count snapshot after each input record and once after completion.
    /// </summary>
    /// <param name="progress">The current deterministic conversion counts.</param>
    void Report(BatchConversionProgress progress);
}
