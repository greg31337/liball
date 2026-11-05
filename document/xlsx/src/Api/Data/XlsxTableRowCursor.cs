using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents a forward cursor over typed worksheet table rows.
/// </summary>
public class XlsxTableRowCursor
{
    /// <summary>
    /// Indicates whether the cursor was opened without errors.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while opening or reading the cursor.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the table name.
    /// </summary>
    public string TableName;
    /// <summary>
    /// Stores the current typed row array.
    /// </summary>
    public XlsxCellValue[]? Current;
    /// <summary>
    /// Stores the zero-based projected row index of the current row.
    /// </summary>
    public int RowIndex;
    /// <summary>
    /// Stores the number of rows read.
    /// </summary>
    public int RowsRead;
    /// <summary>
    /// Indicates whether the cursor has reached the end of the projected rows.
    /// </summary>
    public bool EndOfRows;
    internal List<XlsxCellValue[]> Rows;
    internal int Position;

    /// <summary>
    /// Initializes an empty table row cursor.
    /// </summary>
    public XlsxTableRowCursor()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        TableName = "";
        Current = null;
        RowIndex = -1;
        RowsRead = 0;
        EndOfRows = false;
        Rows = new List<XlsxCellValue[]>();
        Position = 0;
    }

    internal bool Read()
    {
        if (!IsValid || Position >= Rows.Count)
        {
            Current = null;
            EndOfRows = true;
            return false;
        }

        Current = Rows[Position];
        RowIndex = Position;
        Position = Position + 1;
        RowsRead = RowsRead + 1;
        EndOfRows = false;
        return true;
    }

    internal void Reset()
    {
        Current = null;
        RowIndex = -1;
        RowsRead = 0;
        Position = 0;
        EndOfRows = false;
    }
}
