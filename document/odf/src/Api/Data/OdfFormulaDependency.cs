namespace odf;

/// <summary>
/// Describes a dependency referenced by an OpenFormula expression.
/// </summary>
public class OdfFormulaDependency
{
    /// <summary>
    /// The normalized cell or named-expression key, when singular.
    /// </summary>
    public string Key = "";
    /// <summary>
    /// The original dependency text.
    /// </summary>
    public string LexicalValue = "";
    /// <summary>
    /// The sheet containing the formula that owns this dependency.
    /// </summary>
    public string OwnerSheet = "";
    /// <summary>
    /// The structured reference semantics.
    /// </summary>
    public OdfFormulaReference? Reference;
    /// <summary>
    /// Whether this dependency represents a range.
    /// </summary>
    public bool IsRange;
    /// <summary>
    /// Whether this dependency targets an external document.
    /// </summary>
    public bool IsExternal;
}
