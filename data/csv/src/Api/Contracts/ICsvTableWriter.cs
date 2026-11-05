namespace csv;

/// <summary>
/// Writes bounded in-memory CSV tables.
/// </summary>
public interface ICsvTableWriter
{
    /// <summary>Writes an optional configured header followed by every table row.</summary>
    /// <param name="table">The table to write.</param>
    void WriteTable(CsvTable table);
}
