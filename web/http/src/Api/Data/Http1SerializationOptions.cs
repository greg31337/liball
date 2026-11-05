namespace http;

/// <summary>
/// Configures canonical HTTP/1 request or response serialization.
/// </summary>
public class Http1SerializationOptions
{
    /// <summary>
    /// Indicates that the caller selected an explicit body framing after strict
    /// validation of any existing framing fields.
    /// </summary>
    public bool HasBodyFraming;

    /// <summary>
    /// The caller-selected body framing used to normalize validated framing fields
    /// when <see cref="HasBodyFraming"/> is set.
    /// </summary>
    public Http1BodyFraming BodyFraming;

    /// <summary>
    /// Preserves original field-name spelling instead of applying canonical casing.
    /// </summary>
    public bool PreserveFieldNameCasing = true;

    /// <summary>
    /// The maximum content octets emitted in one chunk.
    /// </summary>
    public int ChunkSize = 8192;

    /// <summary>
    /// The validated extension appended to each generated chunk, or <c>null</c> for none.
    /// </summary>
    public string? ChunkExtension;

    /// <summary>
    /// Emits a Trailer field naming all trailer fields when trailers are present.
    /// </summary>
    public bool DeclareTrailers = true;

    /// <summary>
    /// Adds Content-Length when selected framing requires it and the field is absent.
    /// </summary>
    public bool AddContentLength = true;

    /// <summary>
    /// Verifies an existing Content-Length against the serialized content size.
    /// </summary>
    public bool VerifyContentLength = true;

    /// <summary>
    /// Serializes a proxy-style absolute-form request target when applicable.
    /// </summary>
    public bool UseAbsoluteRequestTarget;

    /// <summary>
    /// A validated request-target override, or <c>null</c> to use message control data.
    /// </summary>
    public string? RequestTarget;

    /// <summary>
    /// The request method associated with a response, or <c>null</c> when unknown.
    /// </summary>
    public string? ResponseRequestMethod;

    /// <summary>
    /// Requests connection closure after serialization.
    /// </summary>
    public bool CloseConnection;

    /// <summary>
    /// The resource limits applied during validation and serialization.
    /// </summary>
    public HttpLimits Limits = new HttpLimits();
}
