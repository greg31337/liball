namespace http;

/// <summary>
/// Represents the outcome of HTTP representation negotiation.
/// </summary>
public class HttpNegotiationResult
{
    /// <summary>
    /// The negotiation outcome.
    /// </summary>
    public HttpNegotiationOutcome Outcome;

    /// <summary>
    /// The selected representation, or <c>null</c> when no representation was selected.
    /// </summary>
    public HttpRepresentation? Representation;

    /// <summary>
    /// The selected candidate, or <c>null</c> when no candidate was selected.
    /// </summary>
    public HttpNegotiationCandidate? Candidate;

    /// <summary>
    /// Indicates that media-type quality and specificity were evaluated.
    /// </summary>
    public bool HasMediaMatch;

    /// <summary>
    /// The selected media-type quality in thousandths.
    /// </summary>
    public int MediaQuality;

    /// <summary>
    /// The selected media-type specificity.
    /// </summary>
    public int MediaSpecificity;

    /// <summary>
    /// Indicates that character-set quality and specificity were evaluated.
    /// </summary>
    public bool HasCharsetMatch;

    /// <summary>
    /// The selected character-set quality in thousandths.
    /// </summary>
    public int CharsetQuality;

    /// <summary>
    /// The selected character-set specificity.
    /// </summary>
    public int CharsetSpecificity;

    /// <summary>
    /// Indicates that content-coding quality and specificity were evaluated.
    /// </summary>
    public bool HasEncodingMatch;

    /// <summary>
    /// The selected content-coding quality in thousandths.
    /// </summary>
    public int EncodingQuality;

    /// <summary>
    /// The selected content-coding specificity.
    /// </summary>
    public int EncodingSpecificity;

    /// <summary>
    /// Indicates that language quality and specificity were evaluated.
    /// </summary>
    public bool HasLanguageMatch;

    /// <summary>
    /// The selected language quality in thousandths.
    /// </summary>
    public int LanguageQuality;

    /// <summary>
    /// The selected language specificity.
    /// </summary>
    public int LanguageSpecificity;

    /// <summary>
    /// Indicates that an overall quality score is available.
    /// </summary>
    public bool HasOverallQuality;

    /// <summary>
    /// The combined quality score before deterministic tie breaking.
    /// </summary>
    public long OverallQuality;

    /// <summary>
    /// A caller-readable selection or failure explanation, or <c>null</c> when omitted.
    /// </summary>
    public string? Reason;
}
