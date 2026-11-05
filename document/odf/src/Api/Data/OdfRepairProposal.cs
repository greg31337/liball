namespace odf;

/// <summary>
/// Describes one explicit document repair proposed by the SDK.
/// </summary>
/// <remarks>
/// Apply the original proposal instance returned by <see cref="IOdfRepairService"/>.
/// Unsafe proposals are reported for review but are not applied automatically.
/// </remarks>
public class OdfRepairProposal
{
    /// <summary>
    /// The stable identifier within one repair analysis.
    /// </summary>
    public string Id = "";
    /// <summary>
    /// The repair category.
    /// </summary>
    public OdfRepairKind Kind;
    /// <summary>
    /// The human-readable repair description.
    /// </summary>
    public string Description = "";
    /// <summary>
    /// The location affected by the repair.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
    /// <summary>
    /// Whether the repair is unambiguous and safe to apply.
    /// </summary>
    public bool IsSafe;
    /// <summary>
    /// Whether this proposal has already been applied.
    /// </summary>
    public bool IsApplied;
    /// <summary>
    /// The value observed during analysis, when applicable.
    /// </summary>
    public string BeforeValue = "";
    /// <summary>
    /// The proposed replacement value, when applicable.
    /// </summary>
    public string AfterValue = "";
    internal RepairProposal? CoreProposal;
}
