namespace molecule;


/// <summary>
/// Selects successfully read molecule records for batch output.
/// </summary>
public interface IBatchConversionFilter
{
    /// <summary>
    /// Determines whether a successfully read record is written.
    /// </summary>
    /// <param name="record">The zero-based input record and its read diagnostics.</param>
    /// <returns><c>true</c> to write the record; otherwise, <c>false</c> to count it as skipped.</returns>
    bool Include(BatchConversionRecord record);
}
