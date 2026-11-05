namespace csv;

/// <summary>
/// Supplies rows and reader metadata to streaming CSV operations.
/// </summary>
public interface ICsvRowReader
{
    /// <summary>
    /// Advances to the next available row.
    /// </summary>
    /// <returns><c>true</c> when a row is available; otherwise <c>false</c>.</returns>
    bool Read();

    /// <summary>
    /// Reads or generates the source header.
    /// </summary>
    /// <returns><c>true</c> when a header is available; otherwise <c>false</c>.</returns>
    bool ReadHeader();

    /// <summary>
    /// Returns the row from the latest successful read.
    /// </summary>
    /// <returns>The current row.</returns>
    CsvRow GetCurrentRow();

    /// <summary>
    /// Returns the active header, including generated names.
    /// </summary>
    /// <returns>The active header.</returns>
    CsvHeader GetHeaders();

    /// <summary>
    /// Returns accumulated source diagnostics.
    /// </summary>
    /// <returns>The diagnostic collection.</returns>
    CsvDiagnosticList GetDiagnostics();

    /// <summary>
    /// Indicates whether the source has a header record that should be consumed separately.
    /// </summary>
    /// <returns><c>true</c> when a header record should be read; otherwise <c>false</c>.</returns>
    bool ShouldReadHeader();

}
