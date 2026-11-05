namespace csv;

/// <summary>
/// Receives rejected rows and emits a self-contained reject stream.
/// </summary>
public interface ICsvRejectSink
{
    /// <summary>Writes one rejected row and its diagnostic.</summary>
    /// <param name="row">The rejected row.</param>
    /// <param name="diagnostic">The rejection diagnostic.</param>
    void WriteReject(CsvRow row, CsvDiagnostic diagnostic);

    /// <summary>Flushes buffered reject output.</summary>
    void Flush();

    /// <summary>Closes the reject output according to its ownership settings.</summary>
    void Close();
}
