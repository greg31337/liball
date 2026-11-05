namespace odf;

/// <summary>
/// Reports one mutation performed while applying an explicit repair.
/// </summary>
public class OdfRepairMutation
{
    /// <summary>
    /// The identifier of the proposal that caused the mutation.
    /// </summary>
    public string ProposalId = "";
    /// <summary>
    /// The mutation category.
    /// </summary>
    public OdfRepairKind Kind;
    /// <summary>
    /// The human-readable mutation description.
    /// </summary>
    public string Description = "";
    /// <summary>
    /// The mutated location.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
    /// <summary>
    /// The value before mutation, when applicable.
    /// </summary>
    public string BeforeValue = "";
    /// <summary>
    /// The value after mutation, when applicable.
    /// </summary>
    public string AfterValue = "";
}
