namespace odf;

/// <summary>
/// Describes resolved style inheritance and effective property elements.
/// </summary>
public class OdfStyleResolution
{
    /// <summary>
    /// Whether the style inheritance chain is valid.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The style whose effective properties were resolved.
    /// </summary>
    public OdfStyle? Style;
    /// <summary>
    /// The default, parent, and target styles applied in cascade order.
    /// </summary>
    public OdfStyle[] Sources = [];
    /// <summary>
    /// Independent merged property elements in source order.
    /// </summary>
    public OdfNode[] EffectiveProperties = [];
    /// <summary>
    /// Diagnostics produced while resolving the style.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
}
