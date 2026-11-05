namespace odf;

/// <summary>Describes a cell snapshot stored inside a tracked spreadsheet change.</summary>
public class OdfSpreadsheetChangeCell
{
    /// <summary>The cell address lexical value.</summary>
    public string CellAddress = "";
    /// <summary>Whether this cell is covered by a matrix formula.</summary>
    public bool MatrixCovered;
    /// <summary>The cell formula lexical value.</summary>
    public string Formula = "";
    /// <summary>The number of matrix columns spanned.</summary>
    public long MatrixColumnsSpanned;
    /// <summary>The number of matrix rows spanned.</summary>
    public long MatrixRowsSpanned;
    /// <summary>The office value-type token.</summary>
    public string ValueType = "";
    /// <summary>The value lexical form appropriate for <see cref="ValueType"/>.</summary>
    public string Value = "";
    /// <summary>The currency code, when applicable.</summary>
    public string Currency = "";
    /// <summary>The plain-text projection of the stored cell paragraphs.</summary>
    public string Text = "";
    /// <summary>The underlying <c>table:change-track-table-cell</c> element.</summary>
    public OdfNode? Node;
}
