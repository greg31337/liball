namespace odf;

/// <summary>
/// Describes an outgoing drawing-object, layer, style, or resource reference.
/// </summary>
public class OdfDrawingReference
{
    /// <summary>The reference kind.</summary>
    public OdfDrawingReferenceKind Kind;
    /// <summary>The referenced lexical identifier or IRI.</summary>
    public string Target = "";
    /// <summary>The resolved drawing object, when the target is internal.</summary>
    public OdfDrawingObject? TargetObject;
    /// <summary>Whether the reference targets an external resource.</summary>
    public bool IsExternal;
    /// <summary>Whether an internal target is missing.</summary>
    public bool IsBroken;
    /// <summary>Whether more than one internal target matches.</summary>
    public bool IsAmbiguous;
    /// <summary>The element carrying the reference.</summary>
    public OdfNode? Node;
    internal string OriginalTarget = "";
}
