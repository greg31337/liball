using System.Collections.Generic;

namespace csv;

/// <summary>
/// Provides an ordered, mutable collection of CSV diagnostics.
/// </summary>
public class CsvDiagnosticList
{
    /// <summary>
    /// Diagnostics in insertion order.
    /// </summary>
    public List<CsvDiagnostic> Items;

    /// <summary>
    /// Initializes a new CsvDiagnosticList with library defaults.
    /// </summary>
    public CsvDiagnosticList()
    {
        Items = new List<CsvDiagnostic>();
    }

    /// <summary>
    /// Creates an empty diagnostic list.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvDiagnosticList Create()
    {
        CsvDiagnosticList list = new CsvDiagnosticList();
        return list;
    }

    /// <summary>
    /// Returns the number of stored diagnostics.
    /// </summary>
    /// <returns>The number of items in <see cref="Items"/>.</returns>
    internal int Count()
    {
        int count = Items.Count;
        return count;
    }

    /// <summary>
    /// Checks whether any stored diagnostic is an error.
    /// </summary>
    /// <returns><c>true</c> when at least one item has <see cref="CsvDiagnostic.IsError"/> set; otherwise <c>false</c>.</returns>
    internal bool HasErrors()
    {
        int index = 0;
        while (index < Items.Count)
        {
            CsvDiagnostic diagnostic = Items[index];
            if (diagnostic.IsError)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Adds an item to the end of the collection.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to process.</param>
    internal void Add(CsvDiagnostic diagnostic)
    {
        Items.Add(diagnostic);
    }

    /// <summary>
    /// Returns the item at a zero-based position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The diagnostic at <c>index</c>.</returns>
    internal CsvDiagnostic Get(int index)
    {
        CsvDiagnostic diagnostic = Items[index];
        return diagnostic;
    }

    /// <summary>
    /// Removes every item.
    /// </summary>
    internal void Clear()
    {
        Items.Clear();
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvDiagnosticList Clone()
    {
        CsvDiagnosticList list = new CsvDiagnosticList();
        int index = 0;
        while (index < Items.Count)
        {
            CsvDiagnostic diagnostic = Items[index].Clone();
            list.Add(diagnostic);
            index = index + 1;
        }

        return list;
    }
}
