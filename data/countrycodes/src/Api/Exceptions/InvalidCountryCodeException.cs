namespace countrycodes;

/// <summary>
/// Reports that a supplied country code is invalid for the requested operation.
/// </summary>
public class InvalidCountryCodeException : global::System.Exception
{
    /// <summary>
    /// The invalid input, or null when the caller supplied null.
    /// </summary>
    public string? Input;

    /// <summary>
    /// The country-code kind expected by the operation.
    /// </summary>
    public CountryCodeKind ExpectedKind;

    /// <summary>
    /// The stable category of the validation failure.
    /// </summary>
    public ValidationFailureKind FailureKind;

    /// <summary>
    /// Creates an invalid-country-code exception with stable failure context.
    /// </summary>
    /// <param name="input">The invalid input, or null.</param>
    /// <param name="expectedKind">The country-code kind expected by the operation.</param>
    /// <param name="failureKind">The validation failure category.</param>
    public InvalidCountryCodeException(
        string? input,
        CountryCodeKind expectedKind,
        ValidationFailureKind failureKind)
        : base($"Invalid country code '{(input == null ? "<null>" : input)}' for expected kind {expectedKind}: {failureKind}.")
    {
        Input = input;
        ExpectedKind = expectedKind;
        FailureKind = failureKind;
    }
}
