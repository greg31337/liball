namespace csv;

/// <summary>
/// Provides field-level output and lifecycle control for a CSV writer.
/// </summary>
public interface ICsvWriterControl
{
    /// <summary>
    /// Writes one field to the current record.
    /// </summary>
    /// <param name="value">The field value.</param>
    void WriteField(string? value);

    /// <summary>
    /// Writes a comment record.
    /// </summary>
    /// <param name="value">The comment text.</param>
    void WriteComment(string? value);

    /// <summary>
    /// Completes the current record.
    /// </summary>
    void NextRecord();

    /// <summary>
    /// Flushes buffered output.
    /// </summary>
    void Flush();

    /// <summary>
    /// Closes the writer and, unless configured otherwise, its destination.
    /// </summary>
    void Close();
}
