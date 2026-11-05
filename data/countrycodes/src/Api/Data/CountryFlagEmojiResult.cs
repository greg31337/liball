namespace countrycodes;

/// <summary>
/// Holds the outcome of deriving a regional-indicator flag emoji.
/// </summary>
public class CountryFlagEmojiResult
{
    /// <summary>
    /// Whether a flag emoji is available for a currently assigned country.
    /// </summary>
    public bool IsAvailable;

    /// <summary>
    /// The canonical alpha-2 code when the input has a valid alpha-2 shape.
    /// </summary>
    public string? CanonicalAlpha2;

    /// <summary>
    /// Whether an assignment status was detected from a valid alpha-2 input.
    /// </summary>
    public bool HasAssignmentStatus;

    /// <summary>
    /// The detected assignment status when <see cref="HasAssignmentStatus"/> is true.
    /// </summary>
    public CodeAssignmentStatus AssignmentStatus;

    /// <summary>
    /// The derived regional-indicator flag emoji when <see cref="IsAvailable"/> is true.
    /// </summary>
    public string? FlagEmoji;

    /// <summary>
    /// Creates a detached flag-emoji result.
    /// </summary>
    /// <param name="isAvailable">Whether the emoji is available.</param>
    /// <param name="canonicalAlpha2">The canonical alpha-2 code when available.</param>
    /// <param name="hasAssignmentStatus">Whether an assignment status was detected.</param>
    /// <param name="assignmentStatus">The detected assignment status when available.</param>
    /// <param name="flagEmoji">The derived emoji when available.</param>
    public CountryFlagEmojiResult(
        bool isAvailable,
        string? canonicalAlpha2,
        bool hasAssignmentStatus,
        CodeAssignmentStatus assignmentStatus,
        string? flagEmoji)
    {
        IsAvailable = isAvailable;
        CanonicalAlpha2 = canonicalAlpha2;
        HasAssignmentStatus = hasAssignmentStatus;
        AssignmentStatus = assignmentStatus;
        FlagEmoji = flagEmoji;
    }
}
