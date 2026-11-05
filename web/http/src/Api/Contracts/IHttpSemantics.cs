namespace http;

/// <summary>
/// Provides common HTTP method, status, message, and routing semantics.
/// </summary>
public interface IHttpSemantics
{
    /// <summary>Describes a registered or extension HTTP method.</summary>
    /// <param name="method">The method token.</param>
    /// <returns>The method metadata.</returns>
    HttpMethodMetadata DescribeMethod(string method);

    /// <summary>
    /// Describes a numeric HTTP status code.
    /// </summary>
    /// <param name="statusCode">The numeric status code to describe.</param>
    /// <returns>The status metadata.</returns>
    HttpStatusMetadata DescribeStatus(int statusCode);

    /// <summary>Classifies a numeric HTTP status code.</summary>
    /// <param name="statusCode">The numeric status code.</param>
    /// <returns>The status class.</returns>
    HttpStatusClass ClassifyStatus(int statusCode);

    /// <summary>Determines and validates a request-target form.</summary>
    /// <param name="request">The request control data.</param>
    /// <returns>The request-target form.</returns>
    HttpRequestTargetForm DetermineRequestTarget(HttpRequest request);

    /// <summary>Builds an effective request URI.</summary>
    /// <param name="request">The request and routing metadata.</param>
    /// <returns>The effective absolute URI.</returns>
    string BuildEffectiveRequestUri(HttpRequest request);

    /// <summary>Determines whether response content is permitted for request context.</summary>
    /// <param name="request">The associated request.</param>
    /// <param name="response">The response.</param>
    /// <returns><c>true</c> when response content is permitted.</returns>
    bool DetermineResponseContent(HttpRequest request, HttpResponse response);

    /// <summary>Determines fields that are hop-by-hop for a message.</summary>
    /// <param name="headers">The message fields.</param>
    /// <returns>The fields that must not be forwarded.</returns>
    HttpHeaders DetermineHopFields(HttpHeaders headers);

    /// <summary>Normalizes a request for forwarding by an intermediary.</summary>
    /// <param name="request">The received request.</param>
    /// <returns>The normalized request copy.</returns>
    HttpRequest NormalizeForwardedMessage(HttpRequest request);
}
