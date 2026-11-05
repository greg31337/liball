namespace countrycodes;

/// <summary>
/// Holds one country-code validation issue.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// The stable category of the validation failure.
    /// </summary>
    public ValidationFailureKind Kind;

    /// <summary>
    /// Indicates whether a source position is available.
    /// </summary>
    public bool HasPosition;

    /// <summary>
    /// The zero-based source position when <see cref="HasPosition"/> is true.
    /// </summary>
    public int Position;

    /// <summary>
    /// The affected source length when <see cref="HasPosition"/> is true.
    /// </summary>
    public int Length;

    /// <summary>
    /// The stable programmatic failure message.
    /// </summary>
    public string Message;

    /// <summary>
    /// Creates a validation issue.
    /// </summary>
    /// <param name="kind">The failure category.</param>
    /// <param name="hasPosition">Whether a source position is available.</param>
    /// <param name="position">The zero-based source position when available.</param>
    /// <param name="length">The affected source length when available.</param>
    /// <param name="message">The stable failure message.</param>
    public ValidationIssue(
        ValidationFailureKind kind,
        bool hasPosition,
        int position,
        int length,
        string message)
    {
        Kind = kind;
        HasPosition = hasPosition;
        Position = position;
        Length = length;
        Message = message;
    }

    internal ValidationIssue Copy()
    {
        return new ValidationIssue(Kind, HasPosition, Position, Length, Message);
    }

    internal static ValidationIssue[] CopyArray(ValidationIssue[] values)
    {
        ValidationIssue[] copies = new ValidationIssue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copies[index] = values[index].Copy();
            index += 1;
        }

        return copies;
    }
}
