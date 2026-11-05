namespace odf;

/// <summary>
/// Describes the error state of an OpenFormula result.
/// </summary>
public class OdfFormulaResult
{
    /// <summary>
    /// The semantic value kind.
    /// </summary>
    public OdfValueKind Kind;
    /// <summary>
    /// The numeric value for numeric result kinds.
    /// </summary>
    public double NumericValue;
    /// <summary>
    /// The string value for string results.
    /// </summary>
    public string StringValue = "";
    /// <summary>
    /// The Boolean value for Boolean results.
    /// </summary>
    public bool BooleanValue;
    /// <summary>
    /// The formula error, or <see cref="OdfFormulaError.None"/>.
    /// </summary>
    public OdfFormulaError Error;
    /// <summary>
    /// The row-major array elements or resolved reference values.
    /// </summary>
    public OdfFormulaResult[] Values = [];
    /// <summary>
    /// The number of rows represented by <see cref="Values"/>.
    /// </summary>
    public int RowCount;
    /// <summary>
    /// The number of columns represented by <see cref="Values"/>.
    /// </summary>
    public int ColumnCount;
    /// <summary>
    /// The structured reference for a reference result.
    /// </summary>
    public OdfFormulaReference? Reference;
    /// <summary>
    /// Whether the result has a valid value representation.
    /// </summary>
    public bool IsValid;
}
