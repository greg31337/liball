namespace units;

/// <summary>
/// Collects consistency issues found while validating a unit registry.
/// </summary>
public class RegistryValidationReport : IRegistryValidationReport
{
    internal RegistryValidationIssue[] _issues;

    internal RegistryValidationReport(RegistryValidationIssue[] issues)
    {
        ValidateIssues(issues);
        _issues = CopyIssues(issues);
    }

    /// <summary>
    /// Reports whether no validation issues were found.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsValid()
    {
        return _issues.Length == 0;
    }

    /// <summary>
    /// Reports whether the validation report contains any issues.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasIssues()
    {
        return _issues.Length > 0;
    }

    /// <summary>
    /// Gets the issue count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetIssueCount()
    {
        return _issues.Length;
    }

    /// <summary>
    /// Returns a copy of the issues.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public RegistryValidationIssue[] GetIssues()
    {
        return CopyIssues(_issues);
    }

    /// <summary>
    /// Reports whether the report contains an issue of the supplied kind.
    /// </summary>
    /// <param name="kind">The metadata or result kind.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool ContainsKind(RegistryValidationIssueKind kind)
    {
        int index = 0;

        while (index < _issues.Length)
        {
            if (_issues[index].IsKind(kind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateIssues(RegistryValidationIssue[] issues)
    {
        if (issues == null)
        {
            throw new System.ArgumentNullException("issues");
        }

        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index] == null)
            {
                throw new System.ArgumentNullException("issues");
            }

            index = index + 1;
        }
    }

    internal static RegistryValidationIssue[] CopyIssues(RegistryValidationIssue[] issues)
    {
        RegistryValidationIssue[] copy = new RegistryValidationIssue[issues.Length];
        int index = 0;

        while (index < issues.Length)
        {
            copy[index] = issues[index];
            index = index + 1;
        }

        return copy;
    }
}
