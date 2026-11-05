namespace docx;

/// <summary>
/// Identifies the source of an extracted text token.
/// </summary>
public enum DocxTextTokenKind
{
    /// <summary>
    /// Text from a WordprocessingML text node.
    /// </summary>
    Text = 0,
    /// <summary>
    /// Text produced by a tab control.
    /// </summary>
    Tab = 1,
    /// <summary>
    /// Text produced by a carriage return.
    /// </summary>
    CarriageReturn = 2,
    /// <summary>
    /// Text produced by a line break.
    /// </summary>
    LineBreak = 3,
    /// <summary>
    /// Text produced by a page break.
    /// </summary>
    PageBreak = 4,
    /// <summary>
    /// Text produced by a column break.
    /// </summary>
    ColumnBreak = 5,
    /// <summary>
    /// Text produced by a soft hyphen.
    /// </summary>
    SoftHyphen = 6,
    /// <summary>
    /// Text produced by a non-breaking hyphen.
    /// </summary>
    NoBreakHyphen = 7,
    /// <summary>
    /// Text produced by a symbol control.
    /// </summary>
    Symbol = 8,
    /// <summary>
    /// Text extracted from a field result.
    /// </summary>
    FieldResult = 9,
    /// <summary>
    /// Text extracted from a tracked revision.
    /// </summary>
    RevisionText = 10,
    /// <summary>
    /// A separator between paragraphs or body blocks.
    /// </summary>
    ParagraphSeparator = 11,
    /// <summary>
    /// A separator between document stories.
    /// </summary>
    StorySeparator = 12,
    /// <summary>
    /// Flattened text from a composite story or block.
    /// </summary>
    CompositeText = 13
}
