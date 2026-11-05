namespace countrycodes;

/// <summary>
/// Reports that no country matches a valid country identifier.
/// </summary>
public class CountryNotFoundException : global::System.Exception
{
    /// <summary>
    /// The valid identifier that did not match a country.
    /// </summary>
    public string Input;

    /// <summary>
    /// The detected kind of the country identifier.
    /// </summary>
    public CountryCodeKind Kind;

    /// <summary>
    /// Creates a country-not-found exception with stable lookup context.
    /// </summary>
    /// <param name="input">The valid identifier that was not found.</param>
    /// <param name="kind">The detected identifier kind.</param>
    public CountryNotFoundException(string input, CountryCodeKind kind)
        : base($"No country was found for {kind} code '{input}'.")
    {
        Input = input;
        Kind = kind;
    }
}
