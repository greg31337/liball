namespace uspto;

/// <summary>
/// Normalizes patent and trademark identifiers without performing network access.
/// </summary>
public interface IIdentifierNormalizer
{
    /// <summary>Normalizes a patent-related identifier or reports invalid input.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical identifier text.</returns>
    string NormalizePatentIdentifier(string input);

    /// <summary>Attempts to normalize a patent-related identifier without throwing for invalid input.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical identifier text, or <c>null</c> for invalid input.</returns>
    string? TryNormalizePatentIdentifier(string input);

    /// <summary>Normalizes a trademark identifier or reports invalid input.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical identifier text.</returns>
    string NormalizeTrademarkIdentifier(string input);

    /// <summary>Attempts to normalize a trademark identifier without throwing for invalid input.</summary>
    /// <param name="input">The caller-provided identifier.</param>
    /// <returns>The canonical identifier text, or <c>null</c> for invalid input.</returns>
    string? TryNormalizeTrademarkIdentifier(string input);
}
