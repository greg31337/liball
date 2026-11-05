namespace odf;

/// <summary>
/// Describes the kind of a text field.
/// </summary>
public class OdfTextField
{
    /// <summary>
    /// The text-field kind.
    /// </summary>
    public OdfTextFieldKind Kind;
    /// <summary>
    /// The exact standard field element local name.
    /// </summary>
    public string ElementName = "";
    /// <summary>
    /// The field name, variable name, sequence name, or declaration reference.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The field description.
    /// </summary>
    public string Description = "";
    /// <summary>
    /// The rendered field text retained in the document.
    /// </summary>
    public string DisplayText = "";
    /// <summary>
    /// The field formula.
    /// </summary>
    public string Formula = "";
    /// <summary>
    /// The field condition expression.
    /// </summary>
    public string Condition = "";
    /// <summary>
    /// The conditional true text.
    /// </summary>
    public string TrueValue = "";
    /// <summary>
    /// The conditional false text.
    /// </summary>
    public string FalseValue = "";
    /// <summary>
    /// The preserved current-value flag or current presentation value.
    /// </summary>
    public string CurrentValue = "";
    /// <summary>
    /// The selected conditional text derived from the current-value flag.
    /// </summary>
    public string SelectedText = "";
    /// <summary>
    /// The preserved display behavior.
    /// </summary>
    public string Display = "";
    /// <summary>
    /// The preserved hidden-state value.
    /// </summary>
    public string Hidden = "";
    /// <summary>
    /// Whether the field or its containing paragraph is currently hidden.
    /// </summary>
    public bool IsHidden;
    /// <summary>
    /// The preserved fixed-value flag.
    /// </summary>
    public string Fixed = "";
    /// <summary>
    /// The office value type.
    /// </summary>
    public string ValueType = "";
    /// <summary>
    /// The preserved string value.
    /// </summary>
    public string StringValue = "";
    /// <summary>
    /// The preserved numeric value.
    /// </summary>
    public string Value = "";
    /// <summary>
    /// The preserved currency code.
    /// </summary>
    public string Currency = "";
    /// <summary>
    /// The preserved date or date-time value.
    /// </summary>
    public string DateValue = "";
    /// <summary>
    /// The preserved time or duration value.
    /// </summary>
    public string TimeValue = "";
    /// <summary>
    /// The preserved boolean value.
    /// </summary>
    public string BooleanValue = "";
    /// <summary>
    /// The referenced data-style name.
    /// </summary>
    public string DataStyleName = "";
    /// <summary>
    /// The referenced text-style name.
    /// </summary>
    public string TextStyleName = "";
    /// <summary>
    /// The reference target name.
    /// </summary>
    public string ReferenceName = "";
    /// <summary>
    /// The reference presentation format.
    /// </summary>
    public string ReferenceFormat = "";
    /// <summary>
    /// The placeholder category.
    /// </summary>
    public string PlaceholderType = "";
    /// <summary>
    /// The database name.
    /// </summary>
    public string DatabaseName = "";
    /// <summary>
    /// The database table name.
    /// </summary>
    public string TableName = "";
    /// <summary>
    /// The database table type.
    /// </summary>
    public string TableType = "";
    /// <summary>
    /// The database column name.
    /// </summary>
    public string ColumnName = "";
    /// <summary>
    /// The preserved database row number.
    /// </summary>
    public string RowNumber = "";
    /// <summary>
    /// All qualified field attributes for generic access and preservation.
    /// </summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>
    /// The containing paragraph or heading.
    /// </summary>
    public OdfTextBlock? TextBlock;
    /// <summary>
    /// The corresponding structured text run.
    /// </summary>
    public OdfTextRun? Run;
    /// <summary>
    /// The resolved declaration, when the field references one uniquely.
    /// </summary>
    public OdfTextFieldDeclaration? Declaration;
    /// <summary>
    /// Whether the declaration reference is duplicated or otherwise ambiguous.
    /// </summary>
    public bool HasAmbiguousDeclaration;
    /// <summary>
    /// The XML part containing the field.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying standard field element.
    /// </summary>
    public OdfNode? Node;
    internal string OriginalDisplayText = "";
    internal bool OriginalIsHidden;
}
