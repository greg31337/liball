namespace odf;

/// <summary>
/// Describes a resolved or broken form-control relationship.
/// </summary>
public class OdfFormReference
{
    /// <summary>The relationship kind.</summary>
    public OdfFormReferenceKind Kind;
    /// <summary>The referenced form-control identifier.</summary>
    public string TargetId = "";
    /// <summary>The resolved target control, when present.</summary>
    public OdfFormControl? TargetControl;
    /// <summary>The drawing control shape, when this is a drawing relationship.</summary>
    public OdfDrawingObject? DrawingObject;
    /// <summary>Whether the target could not be resolved.</summary>
    public bool IsBroken;
    /// <summary>The XML node carrying the relationship.</summary>
    public OdfNode? Node;
}
