namespace countrycodes;

/// <summary>
/// Reports that a country identifier matches more than one country.
/// </summary>
public class AmbiguousCountryException : global::System.Exception
{
    /// <summary>
    /// The identifier that produced multiple matches.
    /// </summary>
    public string Input;

    /// <summary>
    /// The canonical alpha-2 codes of all matching countries.
    /// </summary>
    public string[] CandidateCodes;

    /// <summary>
    /// Creates an ambiguous-country exception with all candidate codes.
    /// </summary>
    /// <param name="input">The ambiguous country identifier.</param>
    /// <param name="candidateCodes">The canonical alpha-2 candidate codes.</param>
    public AmbiguousCountryException(string input, string[] candidateCodes)
        : base($"Country identifier '{input}' matched {candidateCodes.Length} countries.")
    {
        Input = input;
        CandidateCodes = (string[])candidateCodes.Clone();
    }
}
