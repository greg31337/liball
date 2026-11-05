namespace whois;

/// <summary>
/// Reports invalid query, endpoint, encoding, limit, or timeout input.
/// </summary>
public class WhoisQueryException : System.Exception
{
    /// <summary>
    /// Identifies the invalid input or option field.
    /// </summary>
    public string InvalidParameter;

    /// <summary>
    /// Contains the caller query text associated with validation.
    /// </summary>
    public string QueryText;

    /// <summary>
    /// Contains the requested query-kind hint.
    /// </summary>
    public WhoisQueryKind QueryKind;

    /// <summary>
    /// Contains the invalid endpoint context, or <c>null</c> when validation concerned another input.
    /// </summary>
    public WhoisEndpoint? Endpoint;

    /// <summary>
    /// Initializes a query validation exception with complete caller-input context.
    /// </summary>
    /// <param name="message">The validation failure message.</param>
    /// <param name="invalidParameter">The invalid input or option field name.</param>
    /// <param name="queryText">The caller query text.</param>
    /// <param name="queryKind">The requested query-kind hint.</param>
    /// <param name="endpoint">The endpoint context, or <c>null</c>.</param>
    /// <param name="innerException">The underlying validation exception, or <c>null</c>.</param>
    public WhoisQueryException(string message, string invalidParameter, string queryText, WhoisQueryKind queryKind, WhoisEndpoint? endpoint, System.Exception? innerException) : base(message, innerException)
    {
        InvalidParameter = invalidParameter;
        QueryText = queryText;
        QueryKind = queryKind;
        Endpoint = endpoint;
    }
}
