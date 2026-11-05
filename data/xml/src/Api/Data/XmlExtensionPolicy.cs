namespace xml;

/// <summary>
/// Carries trust and invocation limits for explicitly registered extensions.
/// </summary>
public class XmlExtensionPolicy
{
    /// <summary>
    /// The extension trust decision.
    /// </summary>
    public XmlExtensionTrust Trust;
    /// <summary>
    /// The namespace URIs permitted when trust is allow-listed.
    /// </summary>
    public string[]? AllowedNamespaceUris;
    /// <summary>
    /// Whether an extension may reenter extension execution through its supplied context.
    /// </summary>
    public bool AllowReentrancy;
    /// <summary>
    /// The maximum extension invocations in one evaluation or transformation; zero uses the implementation default.
    /// </summary>
    public int MaximumInvocations;
}
