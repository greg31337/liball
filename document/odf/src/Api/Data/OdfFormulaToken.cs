namespace odf;

/// <summary>
/// Describes an OpenFormula token.
/// </summary>
public class OdfFormulaToken
{
    /// <summary>
    /// The token kind.
    /// </summary>
    public OdfFormulaTokenKind Kind;
    /// <summary>
    /// The exact token text from the source formula.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// The zero-based UTF-16 source offset.
    /// </summary>
    public int Start;
    /// <summary>
    /// The token length in UTF-16 code units.
    /// </summary>
    public int Length;
}
