namespace http;

/// <summary>
/// Represents a Content-Range value, including an unsatisfied range.
/// </summary>
public class HttpContentRange
{
    /// <summary>
    /// The range unit token.
    /// </summary>
    public string Unit = "";

    /// <summary>
    /// Indicates that an inclusive first position is present.
    /// </summary>
    public bool HasStart;

    /// <summary>
    /// The inclusive first position.
    /// </summary>
    public long Start;

    /// <summary>
    /// Indicates that an inclusive last position is present.
    /// </summary>
    public bool HasEnd;

    /// <summary>
    /// The inclusive last position.
    /// </summary>
    public long End;

    /// <summary>
    /// Indicates that the complete representation length is known.
    /// </summary>
    public bool HasCompleteLength;

    /// <summary>
    /// The complete representation length.
    /// </summary>
    public long CompleteLength;

    /// <summary>
    /// Indicates the unsatisfied-range form with no start or end positions.
    /// </summary>
    public bool IsUnsatisfied;
}
