namespace odf;

/// <summary>
/// Reports one document mutation committed by an edit session.
/// </summary>
public class OdfEditMutation
{
    /// <summary>The staged operation identifier.</summary>
    public string OperationId = "";
    /// <summary>The committed operation kind.</summary>
    public OdfOperationKind Kind;
    /// <summary>The package-relative XML part path.</summary>
    public string PartPath = "";
    /// <summary>The stable identifier of the affected node.</summary>
    public string TargetNodeId = "";
    /// <summary>The value or position before the mutation.</summary>
    public string BeforeValue = "";
    /// <summary>The value or position after the mutation.</summary>
    public string AfterValue = "";
    /// <summary>A human-readable mutation description.</summary>
    public string Description = "";
    /// <summary>The affected document location.</summary>
    public OdfLocation Location = new OdfLocation();
}
