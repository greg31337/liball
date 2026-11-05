namespace countrycodes;

/// <summary>
/// Reports that a supplied subdivision code is invalid.
/// </summary>
public class InvalidSubdivisionCodeException : global::System.Exception
{
    /// <summary>
    /// The invalid input, or null when the caller supplied null.
    /// </summary>
    public string? Input;

    /// <summary>
    /// The stable category of the validation failure.
    /// </summary>
    public ValidationFailureKind FailureKind;

    /// <summary>
    /// Creates an invalid-subdivision-code exception with stable failure context.
    /// </summary>
    /// <param name="input">The invalid input, or null.</param>
    /// <param name="failureKind">The validation failure category.</param>
    public InvalidSubdivisionCodeException(
        string? input,
        ValidationFailureKind failureKind)
        : base($"Invalid subdivision code '{(input == null ? "<null>" : input)}': {failureKind}.")
    {
        Input = input;
        FailureKind = failureKind;
    }
}
