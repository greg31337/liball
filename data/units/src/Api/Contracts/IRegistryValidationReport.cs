namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="RegistryValidationReport"/>.
/// </summary>
public interface IRegistryValidationReport
{
    /// <summary>
    /// Reports whether no validation issues were found.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsValid();
    /// <summary>
    /// Reports whether the validation report contains any issues.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasIssues();
    /// <summary>
    /// Gets the issue count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetIssueCount();
    /// <summary>
    /// Returns a copy of the issues.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public RegistryValidationIssue[] GetIssues();
    /// <summary>
    /// Reports whether the report contains an issue of the supplied kind.
    /// </summary>
    /// <param name="kind">The metadata or result kind.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool ContainsKind(RegistryValidationIssueKind kind);
}
