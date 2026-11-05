namespace whois;

/// <summary>
/// Defines WHOIS query classification and normalization.
/// </summary>
public interface IWhoisQueryClassifier
{
    /// <summary>
    /// Classifies and normalizes caller lookup input.
    /// </summary>
    /// <param name="request">The caller lookup request.</param>
    /// <returns>The classified query and its normalization context.</returns>
    WhoisQuery Classify(WhoisLookupRequest request);
}
