namespace odf;

/// <summary>
/// Describes one endpoint of an OpenFormula cell or range reference.
/// </summary>
public class OdfFormulaReferenceEndpoint
{
    /// <summary>
    /// The explicitly written sheet name, or an empty string.
    /// </summary>
    public string SheetName = "";
    /// <summary>
    /// The sheet name after inheriting it from the first range endpoint.
    /// </summary>
    public string EffectiveSheetName = "";
    /// <summary>
    /// The column label, or an empty string for a whole-row endpoint.
    /// </summary>
    public string ColumnName = "";
    /// <summary>
    /// The one-based column number, or zero for a whole-row endpoint.
    /// </summary>
    public global::System.Numerics.BigInteger ColumnNumber;
    /// <summary>
    /// The one-based row number, or zero for a whole-column endpoint.
    /// </summary>
    public global::System.Numerics.BigInteger RowNumber;
    /// <summary>
    /// Whether the endpoint explicitly names a sheet.
    /// </summary>
    public bool HasSheet;
    /// <summary>
    /// Whether the endpoint contains a column coordinate.
    /// </summary>
    public bool HasColumn;
    /// <summary>
    /// Whether the endpoint contains a row coordinate.
    /// </summary>
    public bool HasRow;
    /// <summary>
    /// Whether the sheet coordinate is absolute.
    /// </summary>
    public bool SheetAbsolute;
    /// <summary>
    /// Whether the column coordinate is absolute.
    /// </summary>
    public bool ColumnAbsolute;
    /// <summary>
    /// Whether the row coordinate is absolute.
    /// </summary>
    public bool RowAbsolute;
}
