namespace odf;

/// <summary>
/// Controls document loading and preservation behavior.
/// </summary>
public class OdfLoadOptions
{
    /// <summary>
    /// Resource limits applied while loading.
    /// </summary>
    public OdfSecurityLimits SecurityLimits = new OdfSecurityLimits();
    /// <summary>
    /// Explicit synchronous cancellation state.
    /// </summary>
    public OdfCancellationState Cancellation = new OdfCancellationState();
    /// <summary>
    /// Optional stream and part selection for partial loading.
    /// </summary>
    public OdfStreamSelection StreamSelection = new OdfStreamSelection();
    /// <summary>
    /// Whether a caller-supplied input stream remains open after the operation.
    /// </summary>
    public bool LeaveInputOpen = true;
    /// <summary>
    /// Whether original lexical values are retained where supported.
    /// </summary>
    public bool PreserveLexicalValues = true;
    /// <summary>
    /// Whether unknown and foreign content is retained.
    /// </summary>
    public bool PreserveUnknownContent = true;
    /// <summary>
    /// Whether XML trees are materialized while loading.
    /// </summary>
    public bool MaterializeXmlTrees = true;
    /// <summary>
    /// Whether package entries are indexed while loading.
    /// </summary>
    public bool IndexPackageEntries = true;
    /// <summary>
    /// Whether validation is performed as part of loading.
    /// </summary>
    public bool ValidateOnLoad = true;
    /// <summary>
    /// Whether documented recovery behavior may be applied to malformed input.
    /// </summary>
    public bool AllowRecovery;
}
