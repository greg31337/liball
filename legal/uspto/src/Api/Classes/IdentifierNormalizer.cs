namespace uspto;

/// <summary>
/// Normalizes patent and trademark identifiers without performing network access.
/// </summary>
public class IdentifierNormalizer : IIdentifierNormalizer
{
    /// <summary>Initializes an offline identifier normalizer.</summary>
    public IdentifierNormalizer()
    {
    }

    /// <summary>Normalizes a patent-related identifier.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical identifier text.</returns>
    /// <exception cref="ValidationException">The identifier is invalid or has multiple valid interpretations.</exception>
    public string NormalizePatentIdentifier(string input)
    {
        IdentifierNormalizationResult result = IdentifierNormalization.NormalizePatent(input);
        if (!result.IsValid)
        {
            string message = "The patent identifier is invalid.";
            if (result.IsAmbiguous)
            {
                message = "The patent identifier is ambiguous; use application or grant formatting to select one interpretation.";
            }

            throw new ValidationException(message, "input", result.FailureCode, null);
        }

        return result.CanonicalNumber;
    }

    /// <summary>Attempts to normalize a patent-related identifier without throwing for invalid input.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical identifier text, or <c>null</c> when the identifier is invalid or ambiguous.</returns>
    public string? TryNormalizePatentIdentifier(string input)
    {
        IdentifierNormalizationResult result = IdentifierNormalization.NormalizePatent(input);
        if (!result.IsValid)
        {
            return null;
        }

        return result.CanonicalNumber;
    }

    /// <summary>Normalizes a trademark identifier.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical TSDR identifier text.</returns>
    /// <exception cref="ValidationException">The identifier is invalid.</exception>
    public string NormalizeTrademarkIdentifier(string input)
    {
        IdentifierNormalizationResult result = IdentifierNormalization.NormalizeTrademark(input);
        if (!result.IsValid)
        {
            throw new ValidationException("The trademark identifier is invalid.", "input", result.FailureCode, null);
        }

        return result.CanonicalNumber;
    }

    /// <summary>Attempts to normalize a trademark identifier without throwing for invalid input.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical TSDR identifier text, or <c>null</c> when the identifier is invalid.</returns>
    public string? TryNormalizeTrademarkIdentifier(string input)
    {
        IdentifierNormalizationResult result = IdentifierNormalization.NormalizeTrademark(input);
        if (!result.IsValid)
        {
            return null;
        }

        return result.CanonicalNumber;
    }
}
