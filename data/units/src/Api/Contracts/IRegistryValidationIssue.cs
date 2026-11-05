namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="RegistryValidationIssue"/>.
/// </summary>
public interface IRegistryValidationIssue
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The resulting registry validation issue kind.</returns>
    public RegistryValidationIssueKind GetKind();
    /// <summary>
    /// Gets the subject id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSubjectId();
    /// <summary>
    /// Gets the detail.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDetail();
    /// <summary>
    /// Gets the developer-readable result message, which is empty for success.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetMessage();
    /// <summary>
    /// Reports whether the issue has the supplied kind.
    /// </summary>
    /// <param name="kind">The metadata or result kind.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsKind(RegistryValidationIssueKind kind);
}
