using System.Collections.Generic;

namespace csv;

/// <summary>
/// Stores rows that share the same grouping key.
/// </summary>
public class CsvGroup
{
    /// <summary>
    /// The grouping key, which may represent a null field.
    /// </summary>
    public string? Key;
    /// <summary>
    /// Rows held by this object.
    /// </summary>
    public List<CsvRow> Rows;
    /// <summary>
    /// The number of contained or aggregated items.
    /// </summary>
    public int Count;

    /// <summary>
    /// Initializes a new CsvGroup with library defaults.
    /// </summary>
    public CsvGroup()
    {
        Key = "";
        Rows = new List<CsvRow>();
        Count = 0;
    }

    /// <summary>
    /// Creates an initialized instance from the supplied values.
    /// </summary>
    /// <param name="key">The grouping key, which may be null.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvGroup Create(string? key)
    {
        CsvGroup group = new CsvGroup();
        group.Key = key;
        return group;
    }

    /// <summary>
    /// Appends a row and increments <see cref="Count"/>.
    /// </summary>
    /// <param name="row">The row to process.</param>
    internal void AddRow(CsvRow row)
    {
        Rows.Add(row);
        Count = Count + 1;
    }
}
