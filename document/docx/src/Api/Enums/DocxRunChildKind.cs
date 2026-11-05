namespace docx;

/// <summary>
/// Identifies the kind of a direct run child.
/// </summary>
public enum DocxRunChildKind
{
    /// <summary>
    /// A text node.
    /// </summary>
    Text = 0,
    /// <summary>
    /// A tab control.
    /// </summary>
    Tab = 1,
    /// <summary>
    /// A carriage-return control.
    /// </summary>
    CarriageReturn = 2,
    /// <summary>
    /// A line, page, or column break.
    /// </summary>
    Break = 3,
    /// <summary>
    /// A soft or non-breaking hyphen.
    /// </summary>
    Hyphen = 4,
    /// <summary>
    /// A symbol control.
    /// </summary>
    Symbol = 5,
    /// <summary>
    /// A complex-field character.
    /// </summary>
    FieldCharacter = 6,
    /// <summary>
    /// Complex-field instruction text.
    /// </summary>
    FieldInstruction = 7,
    /// <summary>
    /// A drawing, picture, chart, diagram, or embedded object.
    /// </summary>
    Drawing = 8,
    /// <summary>
    /// A tracked-revision child.
    /// </summary>
    Revision = 9,
    /// <summary>
    /// A preserved child not represented by another kind.
    /// </summary>
    PreservedUnknown = 10
}
