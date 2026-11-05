using System.Collections.Generic;

namespace http;

/// <summary>
/// Selects HTTP representations and constructs normalized Vary keys.
/// </summary>
public class HttpContentNegotiator : IHttpContentNegotiator
{
    /// <summary>Creates an HTTP content-negotiation service.</summary>
    public HttpContentNegotiator()
    {
    }

    /// <summary>Selects by media type.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The media selection result.</returns>
    public HttpNegotiationResult SelectMediaType(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        return NegotiationMatcher.SelectMedia(request, candidates);
    }

    /// <summary>Selects by character set.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The character-set selection result.</returns>
    public HttpNegotiationResult SelectCharset(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        return NegotiationMatcher.SelectCharset(request, candidates);
    }

    /// <summary>Selects by content coding.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The content-coding selection result.</returns>
    public HttpNegotiationResult SelectContentCoding(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        return NegotiationMatcher.SelectEncoding(request, candidates);
    }

    /// <summary>Selects by content language using RFC 4647 basic filtering.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The language selection result.</returns>
    public HttpNegotiationResult SelectLanguage(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        return NegotiationMatcher.SelectLanguage(request, candidates);
    }

    /// <summary>Selects across all supplied proactive negotiation dimensions.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The combined representation-selection result.</returns>
    public HttpNegotiationResult SelectRepresentation(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        return NegotiationMatcher.SelectRepresentation(request, candidates);
    }

    /// <summary>Builds a normalized Vary selection key.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="varyFields">The parsed Vary field names.</param>
    /// <returns>The normalized selection key.</returns>
    public string BuildVaryKey(HttpRequest request, HttpHeaders varyFields)
    {
        return VarySemantics.BuildKey(request, varyFields);
    }

    /// <summary>Compares two normalized Vary keys.</summary>
    /// <param name="storedKey">The stored key.</param>
    /// <param name="requestKey">The current request key.</param>
    /// <returns><c>true</c> when the keys match.</returns>
    public bool CompareVaryKey(string storedKey, string requestKey)
    {
        return VarySemantics.CompareKeys(storedKey, requestKey);
    }
}
