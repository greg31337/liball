namespace csv;

/// <summary>
/// Receives rows rejected by validation.
/// </summary>
public interface ICsvRejectHandler
{
    /// <summary>
    /// Handles one rejected row and its diagnostic.
    /// </summary>
    /// <param name="row">The rejected row.</param>
    /// <param name="diagnostic">The validation diagnostic.</param>
    void Handle(CsvRow row, CsvDiagnostic diagnostic);
}
