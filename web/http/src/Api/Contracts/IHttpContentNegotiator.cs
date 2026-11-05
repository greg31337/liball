using System.Collections.Generic;

namespace http;

/// <summary>
/// Selects representations using HTTP proactive negotiation rules.
/// </summary>
public interface IHttpContentNegotiator
{
    /// <summary>Selects by media type.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The media selection result.</returns>
    HttpNegotiationResult SelectMediaType(HttpRequest request, List<HttpNegotiationCandidate> candidates);

    /// <summary>Selects by character set.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The character-set selection result.</returns>
    HttpNegotiationResult SelectCharset(HttpRequest request, List<HttpNegotiationCandidate> candidates);

    /// <summary>Selects by content coding.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The content-coding selection result.</returns>
    HttpNegotiationResult SelectContentCoding(HttpRequest request, List<HttpNegotiationCandidate> candidates);

    /// <summary>Selects by content language.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The language selection result.</returns>
    HttpNegotiationResult SelectLanguage(HttpRequest request, List<HttpNegotiationCandidate> candidates);

    /// <summary>
    /// Selects a representation for a request.
    /// </summary>
    /// <param name="request">The request containing the selecting preferences.</param>
    /// <param name="candidates">The available candidates.</param>
    /// <returns>The representation-selection result.</returns>
    HttpNegotiationResult SelectRepresentation(HttpRequest request, List<HttpNegotiationCandidate> candidates);

    /// <summary>Builds a normalized Vary selection key.</summary>
    /// <param name="request">The selecting request.</param>
    /// <param name="varyFields">The parsed Vary field names.</param>
    /// <returns>The normalized selection key.</returns>
    string BuildVaryKey(HttpRequest request, HttpHeaders varyFields);

    /// <summary>Compares two normalized Vary keys.</summary>
    /// <param name="storedKey">The stored key.</param>
    /// <param name="requestKey">The current request key.</param>
    /// <returns><c>true</c> when the keys match.</returns>
    bool CompareVaryKey(string storedKey, string requestKey);
}
