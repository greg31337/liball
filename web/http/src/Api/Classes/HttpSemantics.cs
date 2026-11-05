namespace http;

/// <summary>
/// Provides HTTP method, status, routing, content, and intermediary semantics.
/// </summary>
public class HttpSemantics : IHttpSemantics
{
    /// <summary>Creates an HTTP semantics service.</summary>
    public HttpSemantics()
    {
    }

    /// <summary>Describes a registered or extension HTTP method.</summary>
    /// <param name="method">The method token.</param>
    /// <returns>The method metadata.</returns>
    public HttpMethodMetadata DescribeMethod(string method)
    {
        return MethodSemantics.Describe(method);
    }

    /// <summary>Describes a numeric HTTP status code.</summary>
    /// <param name="statusCode">The numeric status code.</param>
    /// <returns>The status metadata.</returns>
    public HttpStatusMetadata DescribeStatus(int statusCode)
    {
        return StatusSemantics.Describe(statusCode);
    }

    /// <summary>Classifies a numeric HTTP status code.</summary>
    /// <param name="statusCode">The numeric status code.</param>
    /// <returns>The status class.</returns>
    public HttpStatusClass ClassifyStatus(int statusCode)
    {
        return StatusSemantics.Classify(statusCode);
    }

    /// <summary>Determines and validates a request-target form.</summary>
    /// <param name="request">The request control data.</param>
    /// <returns>The request-target form.</returns>
    public HttpRequestTargetForm DetermineRequestTarget(HttpRequest request)
    {
        return RequestTargetSemantics.Determine(request);
    }

    /// <summary>Builds an effective absolute request URI.</summary>
    /// <param name="request">The request and routing metadata.</param>
    /// <returns>The effective absolute URI.</returns>
    public string BuildEffectiveRequestUri(HttpRequest request)
    {
        return RequestTargetSemantics.BuildEffectiveUri(request);
    }

    /// <summary>Determines whether response content is permitted.</summary>
    /// <param name="request">The associated request.</param>
    /// <param name="response">The response.</param>
    /// <returns><c>true</c> when response content is permitted.</returns>
    public bool DetermineResponseContent(HttpRequest request, HttpResponse response)
    {
        return MessageSemantics.AllowsResponseContent(request, response);
    }

    /// <summary>Determines fields that are scoped to one connection hop.</summary>
    /// <param name="headers">The message fields.</param>
    /// <returns>Independent copies of the fields that must not be forwarded.</returns>
    public HttpHeaders DetermineHopFields(HttpHeaders headers)
    {
        return IntermediarySemantics.DetermineHopFields(headers);
    }

    /// <summary>Creates a normalized request copy suitable for forwarding.</summary>
    /// <param name="request">The received request.</param>
    /// <returns>The normalized request copy.</returns>
    public HttpRequest NormalizeForwardedMessage(HttpRequest request)
    {
        return IntermediarySemantics.NormalizeRequest(request);
    }
}
