namespace countrycodes;

/// <summary>
/// Holds the outcome of country-code normalization.
/// </summary>
public class CodeNormalizationResult
{
    /// <summary>
    /// The outcome status of normalization.
    /// </summary>
    public NormalizationStatus Status;

    /// <summary>
    /// The detected kind of code.
    /// </summary>
    public CountryCodeKind Kind;

    /// <summary>
    /// Indicates whether an assignment status was detected.
    /// </summary>
    public bool HasAssignmentStatus;

    /// <summary>
    /// The detected assignment status when <see cref="HasAssignmentStatus"/> is true.
    /// </summary>
    public CodeAssignmentStatus AssignmentStatus;

    /// <summary>
    /// The canonical value when normalization produced one.
    /// </summary>
    public string? CanonicalValue;

    /// <summary>
    /// All deterministic replacement candidates.
    /// </summary>
    public string[] ReplacementCandidates;

    /// <summary>
    /// Creates a detached code normalization result.
    /// </summary>
    /// <param name="status">The normalization outcome.</param>
    /// <param name="kind">The detected code kind.</param>
    /// <param name="hasAssignmentStatus">Whether an assignment status was detected.</param>
    /// <param name="assignmentStatus">The detected assignment status when available.</param>
    /// <param name="canonicalValue">The canonical value when available.</param>
    /// <param name="replacementCandidates">The replacement candidates.</param>
    public CodeNormalizationResult(
        NormalizationStatus status,
        CountryCodeKind kind,
        bool hasAssignmentStatus,
        CodeAssignmentStatus assignmentStatus,
        string? canonicalValue,
        string[] replacementCandidates)
    {
        Status = status;
        Kind = kind;
        HasAssignmentStatus = hasAssignmentStatus;
        AssignmentStatus = assignmentStatus;
        CanonicalValue = canonicalValue;
        ReplacementCandidates = (string[])replacementCandidates.Clone();
    }
}
