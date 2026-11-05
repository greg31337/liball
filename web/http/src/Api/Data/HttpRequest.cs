using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents a complete or incrementally assembled HTTP request.
/// </summary>
public class HttpRequest
{
    /// <summary>
    /// The protocol version used by the request.
    /// </summary>
    public HttpProtocolVersion ProtocolVersion;

    /// <summary>
    /// The case-sensitive request method token.
    /// </summary>
    public string Method = "";

    /// <summary>
    /// The resolved method metadata, or <c>null</c> when semantic lookup has not been performed.
    /// </summary>
    public HttpMethodMetadata? MethodMetadata;

    /// <summary>
    /// The request target exactly as supplied by the protocol mapping.
    /// </summary>
    public string Target = "";

    /// <summary>
    /// The form of the request target.
    /// </summary>
    public HttpRequestTargetForm TargetForm;

    /// <summary>
    /// The URI scheme, or <c>null</c> when it is not present in control data.
    /// </summary>
    public string? Scheme;

    /// <summary>
    /// The URI authority, or <c>null</c> when it is not present in control data.
    /// </summary>
    public string? Authority;

    /// <summary>
    /// The URI path, or <c>null</c> when the selected target form has no path.
    /// </summary>
    public string? Path;

    /// <summary>
    /// Indicates whether a query component is present, including an empty query.
    /// </summary>
    public bool HasQuery;

    /// <summary>
    /// The URI query without its leading question mark, or <c>null</c> when absent.
    /// </summary>
    public string? Query;

    /// <summary>
    /// The extended CONNECT protocol, or <c>null</c> for other requests.
    /// </summary>
    public string? ConnectProtocol;

    /// <summary>
    /// The ordered request header fields.
    /// </summary>
    public HttpHeaders Headers = new HttpHeaders();

    /// <summary>
    /// The parsed weighted field values used for non-language proactive negotiation.
    /// </summary>
    public List<HttpWeightedValue> WeightedPreferences = new List<HttpWeightedValue>();

    /// <summary>
    /// The parsed language ranges used for proactive negotiation.
    /// </summary>
    public List<HttpLanguageRange> LanguagePreferences = new List<HttpLanguageRange>();

    /// <summary>
    /// The parsed authentication credentials or challenges associated with the request.
    /// </summary>
    public List<HttpAuthenticationValue> AuthenticationValues = new List<HttpAuthenticationValue>();

    /// <summary>
    /// The parsed Via entries in received order.
    /// </summary>
    public List<HttpViaEntry> ViaEntries = new List<HttpViaEntry>();

    /// <summary>
    /// The parsed extensible priority, or <c>null</c> when no priority signal is available.
    /// </summary>
    public HttpPriority? Priority;

    /// <summary>
    /// Indicates that an Expect field contains the <c>100-continue</c> expectation.
    /// </summary>
    public bool HasContinueExpectation;

    /// <summary>
    /// Indicates that a 100 (Continue) response is applicable before request content.
    /// </summary>
    public bool ShouldSendContinue;

    /// <summary>
    /// Indicates that an Expect field contains an extension or unsupported expectation.
    /// </summary>
    public bool HasUnsupportedExpectations;

    /// <summary>
    /// Indicates that this HTTP/1.1 request proposes a protocol upgrade.
    /// </summary>
    public bool IsUpgradeRequested;

    /// <summary>
    /// The requested upgrade protocols in preference order.
    /// </summary>
    public List<string> UpgradeProtocols = new List<string>();

    /// <summary>
    /// The owned request content bytes.
    /// </summary>
    public byte[] Content = Array.Empty<byte>();

    /// <summary>
    /// The ordered request trailer fields.
    /// </summary>
    public HttpHeaders Trailers = new HttpHeaders();

    /// <summary>
    /// Indicates that the entire request, including content and trailers, is available.
    /// </summary>
    public bool IsComplete;
}
