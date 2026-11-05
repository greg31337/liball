using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides vendor metadata data and operations for molecular graphs.
/// </summary>
public class VendorMetadata
{
    /// <summary>
    /// The format.
    /// </summary>
    public VendorFormat Format;
    /// <summary>
    /// The headers.
    /// </summary>
    public List<string> Headers;
    /// <summary>
    /// The rows.
    /// </summary>
    public List<List<string>> Rows;
    /// <summary>
    /// The molecule column index.
    /// </summary>
    public int MoleculeColumnIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="VendorMetadata"/> class.
    /// </summary>
    public VendorMetadata()
    {
        Format = new VendorFormat();
        Headers = new List<string>();
        Rows = new List<List<string>>();
        MoleculeColumnIndex = -1;
    }

    internal string GetHeader(int index)
    {
        if (index < 0 || index >= Headers.Count)
        {
            return "";
        }

        return Headers[index];
    }

    internal string GetValue(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            return "";
        }

        List<string> row = Rows[rowIndex];
        if (columnIndex < 0 || columnIndex >= row.Count)
        {
            return "";
        }

        return row[columnIndex];
    }

    internal int GetRowCount()
    {
        return Rows.Count;
    }

    internal int GetColumnCount()
    {
        return Headers.Count;
    }
}

