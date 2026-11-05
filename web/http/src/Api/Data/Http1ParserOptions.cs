namespace http;

/// <summary>
/// Configures HTTP/1 request or response parsing and explicit compatibility policies.
/// </summary>
public class Http1ParserOptions
{
    /// <summary>
    /// The kind of message expected from the input.
    /// </summary>
    public HttpMessageKind MessageKind;

    /// <summary>
    /// The request method associated with a response, or <c>null</c> when unknown.
    /// </summary>
    public string? ResponseRequestMethod;

    /// <summary>
    /// Indicates that the parser is acting as an intermediary recipient.
    /// </summary>
    public bool IsProxyRecipient;

    /// <summary>
    /// Allows one empty line before a request line as permitted by RFC 9112.
    /// </summary>
    public bool AllowLeadingEmptyRequestLine = true;

    /// <summary>
    /// Allows obsolete folded field lines to be normalized.
    /// </summary>
    public bool AllowObsoleteLineFolding;

    /// <summary>
    /// Allows a bare line-feed octet to terminate a line.
    /// </summary>
    public bool AllowBareLineFeed;

    /// <summary>
    /// Allows whitespace between a field name and its colon.
    /// </summary>
    public bool AllowWhitespaceBeforeColon;

    /// <summary>
    /// Allows a message containing both Transfer-Encoding and Content-Length.
    /// </summary>
    public bool AllowTransferEncodingWithContentLength;

    /// <summary>
    /// Allows divergent duplicate Content-Length field values.
    /// </summary>
    public bool AllowMismatchedContentLengths;

    /// <summary>
    /// Allows chunked to appear before the final transfer coding.
    /// </summary>
    public bool AllowChunkedNotFinal;

    /// <summary>
    /// Allows bytes after a close-delimited or terminal message.
    /// </summary>
    public bool AllowPostCloseData;

    /// <summary>
    /// The resource limits applied by the parser.
    /// </summary>
    public HttpLimits Limits = new HttpLimits();
}
