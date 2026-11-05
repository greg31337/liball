namespace odf;

/// <summary>
/// Describes one schema-rule difference between adjacent ODF versions.
/// </summary>
public class OdfVersionChange
{
    /// <summary>
    /// The schema profile containing the rule.
    /// </summary>
    public string Profile = "";
    /// <summary>
    /// How the rule changed.
    /// </summary>
    public OdfVersionChangeKind Kind;
    /// <summary>
    /// The category of the affected schema construct.
    /// </summary>
    public OdfVersionChangeCategory Category;
    /// <summary>
    /// The namespace name of the affected construct, when applicable.
    /// </summary>
    public string NamespaceName = "";
    /// <summary>
    /// The local or definition name of the affected construct.
    /// </summary>
    public string Name = "";
}
