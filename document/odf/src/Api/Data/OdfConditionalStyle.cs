namespace odf;

/// <summary>Describes a conditional map owned by an ordinary OpenDocument style.</summary>
public class OdfConditionalStyle
{
    /// <summary>The name of the style that owns the condition.</summary>
    public string OwnerStyleName = "";
    /// <summary>The lexical family name of the style that owns the condition.</summary>
    public string OwnerStyleFamily = "";
    /// <summary>The XML part containing the owning style.</summary>
    public string PartPath = "";
    /// <summary>Whether the owning style is an automatic style.</summary>
    public bool IsAutomatic;
    /// <summary>The condition that activates the mapped style.</summary>
    public string Condition = "";
    /// <summary>The style name applied when the condition is true.</summary>
    public string ApplyStyleName = "";
    /// <summary>The optional base cell address used by the condition.</summary>
    public string BaseCellAddress = "";
    /// <summary>The resolved style that owns the condition.</summary>
    public OdfStyle? OwnerStyle;
    /// <summary>The resolved style applied by the condition.</summary>
    public OdfStyle? ApplyStyle;
    /// <summary>The underlying style-map element.</summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfNode? OriginalOwnerNode;
}
