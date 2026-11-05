namespace odf;

/// <summary>
/// Describes the enum-valued characteristics of a cell or formula value.
/// </summary>
public class OdfCellValue
{
    /// <summary>
    /// The semantic value kind.
    /// </summary>
    public OdfValueKind Kind;
    /// <summary>
    /// How the value was obtained.
    /// </summary>
    public OdfValueProvenance Provenance;
    /// <summary>
    /// The value unit, when applicable.
    /// </summary>
    public OdfUnitKind UnitKind;
    /// <summary>
    /// The exact <c>office:value-type</c> token, when present.
    /// </summary>
    public string ValueType = "";
    /// <summary>
    /// The preserved lexical representation of the typed value.
    /// </summary>
    public string LexicalValue = "";
    /// <summary>
    /// The string value, when <see cref="Kind"/> is string or error.
    /// </summary>
    public string StringValue = "";
    /// <summary>
    /// The numeric value for float, percentage, and currency values.
    /// </summary>
    public double NumericValue;
    /// <summary>
    /// The ISO currency code for a currency value.
    /// </summary>
    public string Currency = "";
    /// <summary>
    /// The preserved XML Schema date or date-time value.
    /// </summary>
    public string DateValue = "";
    /// <summary>
    /// The preserved XML Schema duration value.
    /// </summary>
    public string DurationValue = "";
    /// <summary>
    /// The Boolean value, when <see cref="Kind"/> is Boolean.
    /// </summary>
    public bool BooleanValue;
    /// <summary>
    /// The formula error represented by this value, when present.
    /// </summary>
    public OdfFormulaError Error;
    /// <summary>
    /// Whether the source lexical value is valid for its declared type.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Whether this value is the cached result of a cell formula.
    /// </summary>
    public bool IsCachedFormulaResult;
    /// <summary>
    /// The cell that owns the value.
    /// </summary>
    public OdfCell? Cell;
    internal OdfValueKind OriginalKind;
    internal string OriginalValueType = "";
    internal string OriginalLexicalValue = "";
    internal string OriginalStringValue = "";
    internal double OriginalNumericValue;
    internal string OriginalCurrency = "";
    internal string OriginalDateValue = "";
    internal string OriginalDurationValue = "";
    internal bool OriginalBooleanValue;
    internal OdfFormulaError OriginalError;
    internal bool OriginalIsValid;
}
