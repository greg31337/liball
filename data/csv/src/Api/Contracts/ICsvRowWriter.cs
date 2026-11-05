namespace csv;

/// <summary>
/// Receives headers and rows from streaming CSV operations.
/// </summary>
public interface ICsvRowWriter
{
    /// <summary>
    /// Writes one header record.
    /// </summary>
    /// <param name="header">The header to write.</param>
    void WriteHeader(CsvHeader header);

    /// <summary>
    /// Writes one data row.
    /// </summary>
    /// <param name="row">The row to write.</param>
    void WriteRow(CsvRow row);

    /// <summary>
    /// Indicates whether a header should be written when one is available.
    /// </summary>
    /// <returns><c>true</c> when a header should be written; otherwise <c>false</c>.</returns>
    bool ShouldWriteHeader();

}
