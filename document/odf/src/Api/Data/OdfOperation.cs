namespace odf;

/// <summary>Describes one ordered, replayable collaboration operation.</summary>
/// <remarks>
/// Operation logs are SDK extension data and are not standard OpenDocument
/// tracked-change elements.
/// </remarks>
public class OdfOperation
{
    /// <summary>The operation identifier.</summary>
    public string Id = "";
    /// <summary>The signed order key used during stable replay.</summary>
    public long Sequence;
    /// <summary>The operation kind.</summary>
    public OdfOperationKind Kind;
    /// <summary>The XML part path, or an empty string for a flat document.</summary>
    public string PartPath = "";
    /// <summary>The stable identifier of the target node.</summary>
    public string TargetNodeId = "";
    /// <summary>The stable identifier of the destination or insertion parent.</summary>
    public string ParentNodeId = "";
    /// <summary>The destination or insertion child index.</summary>
    public int ChildIndex = -1;
    /// <summary>The stable identifier of the previous parent.</summary>
    public string PreviousParentNodeId = "";
    /// <summary>The previous child index.</summary>
    public int PreviousChildIndex = -1;
    /// <summary>The attribute affected by an attribute operation.</summary>
    public OdfQualifiedName? AttributeName;
    /// <summary>The new attribute or text value.</summary>
    public string Value = "";
    /// <summary>Whether a previous attribute or text value was captured.</summary>
    public bool HadPreviousValue;
    /// <summary>The captured previous attribute or text value.</summary>
    public string PreviousValue = "";
    /// <summary>The operation author.</summary>
    public string Author = "";
    /// <summary>The operation session identifier.</summary>
    public string SessionId = "";
    /// <summary>The operation date or date-time lexical value.</summary>
    public string Date = "";
    /// <summary>The conditions checked before replay.</summary>
    public OdfEditPrecondition[] Preconditions = [];
    /// <summary>The captured subtree used by insertion, removal, or inversion.</summary>
    public OdfNode? Payload;
    /// <summary>An unknown preserved extension operation element.</summary>
    public OdfNode? ExtensionNode;
}
