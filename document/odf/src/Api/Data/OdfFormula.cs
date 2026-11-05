namespace odf;

/// <summary>
/// Describes the enum-valued structure of an OpenFormula expression.
/// </summary>
public class OdfFormula
{
    /// <summary>
    /// The original formula source text.
    /// </summary>
    public string Source = "";
    /// <summary>
    /// The lexical formula namespace prefix, or an empty string.
    /// </summary>
    public string NamespacePrefix = "";
    /// <summary>
    /// Whether the formula requests forced recalculation.
    /// </summary>
    public bool ForcedRecalculation;
    /// <summary>
    /// The required OpenFormula conformance level.
    /// </summary>
    public OdfFormulaConformance Conformance;
    /// <summary>
    /// The lexical tokens.
    /// </summary>
    public OdfFormulaToken[] Tokens = [];
    /// <summary>
    /// The root syntax-tree node, when parsed.
    /// </summary>
    public OdfFormulaNode? Root;
    /// <summary>
    /// Structured lexical and parse diagnostics.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
    /// <summary>
    /// Whether tokenization and parsing completed without an error diagnostic.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The most recent result, when evaluated.
    /// </summary>
    public OdfFormulaResult? Result;
    /// <summary>
    /// The cell, range, and named-expression dependencies.
    /// </summary>
    public OdfFormulaDependency[] Dependencies = [];
    internal string OriginalNamespacePrefix = "";
    internal bool OriginalForcedRecalculation;
    internal OdfFormulaNode? OriginalRoot;
}
