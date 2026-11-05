namespace odf;

/// <summary>Describes a condition checked immediately before operation replay.</summary>
public class OdfEditPrecondition
{
    /// <summary>The precondition kind.</summary>
    public OdfEditPreconditionKind Kind;
    /// <summary>The stable identifier of the target node.</summary>
    public string TargetNodeId = "";
    /// <summary>The stable identifier of the expected parent node.</summary>
    public string ParentNodeId = "";
    /// <summary>The expected zero-based child index.</summary>
    public int ChildIndex = -1;
    /// <summary>The selected attribute name.</summary>
    public OdfQualifiedName? AttributeName;
    /// <summary>Whether the selected value is expected to be present.</summary>
    public bool ExpectedValuePresent;
    /// <summary>The expected attribute or text value.</summary>
    public string ExpectedValue = "";
}
