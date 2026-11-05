namespace http;

/// <summary>
/// Provides conventional HTTP reason phrases without assigning them protocol semantics.
/// </summary>
public static class HttpReasonPhrases
{
    /// <summary>The conventional reason phrase for status 100.</summary>
    public const string Continue = "Continue";
    /// <summary>The conventional reason phrase for status 101.</summary>
    public const string SwitchingProtocols = "Switching Protocols";
    /// <summary>The conventional reason phrase for status 102.</summary>
    public const string Processing = "Processing";
    /// <summary>The conventional reason phrase for status 103.</summary>
    public const string EarlyHints = "Early Hints";
    /// <summary>The conventional reason phrase for status 104.</summary>
    public const string UploadResumptionSupported = "Upload Resumption Supported";
    /// <summary>The conventional reason phrase for status 200.</summary>
    public const string Ok = "OK";
    /// <summary>The conventional reason phrase for status 201.</summary>
    public const string Created = "Created";
    /// <summary>The conventional reason phrase for status 202.</summary>
    public const string Accepted = "Accepted";
    /// <summary>The conventional reason phrase for status 203.</summary>
    public const string NonAuthoritativeInformation = "Non-Authoritative Information";
    /// <summary>The conventional reason phrase for status 204.</summary>
    public const string NoContent = "No Content";
    /// <summary>The conventional reason phrase for status 205.</summary>
    public const string ResetContent = "Reset Content";
    /// <summary>The conventional reason phrase for status 206.</summary>
    public const string PartialContent = "Partial Content";
    /// <summary>The conventional reason phrase for status 207.</summary>
    public const string MultiStatus = "Multi-Status";
    /// <summary>The conventional reason phrase for status 208.</summary>
    public const string AlreadyReported = "Already Reported";
    /// <summary>The conventional reason phrase for status 226.</summary>
    public const string ImUsed = "IM Used";
    /// <summary>The conventional reason phrase for status 300.</summary>
    public const string MultipleChoices = "Multiple Choices";
    /// <summary>The conventional reason phrase for status 301.</summary>
    public const string MovedPermanently = "Moved Permanently";
    /// <summary>The conventional reason phrase for status 302.</summary>
    public const string Found = "Found";
    /// <summary>The conventional reason phrase for status 303.</summary>
    public const string SeeOther = "See Other";
    /// <summary>The conventional reason phrase for status 304.</summary>
    public const string NotModified = "Not Modified";
    /// <summary>The conventional reason phrase for status 305.</summary>
    public const string UseProxy = "Use Proxy";
    /// <summary>The conventional reason phrase for unused status 306.</summary>
    public const string Unused306 = "(Unused)";
    /// <summary>The conventional reason phrase for status 307.</summary>
    public const string TemporaryRedirect = "Temporary Redirect";
    /// <summary>The conventional reason phrase for status 308.</summary>
    public const string PermanentRedirect = "Permanent Redirect";
    /// <summary>The conventional reason phrase for status 400.</summary>
    public const string BadRequest = "Bad Request";
    /// <summary>The conventional reason phrase for status 401.</summary>
    public const string Unauthorized = "Unauthorized";
    /// <summary>The conventional reason phrase for status 402.</summary>
    public const string PaymentRequired = "Payment Required";
    /// <summary>The conventional reason phrase for status 403.</summary>
    public const string Forbidden = "Forbidden";
    /// <summary>The conventional reason phrase for status 404.</summary>
    public const string NotFound = "Not Found";
    /// <summary>The conventional reason phrase for status 405.</summary>
    public const string MethodNotAllowed = "Method Not Allowed";
    /// <summary>The conventional reason phrase for status 406.</summary>
    public const string NotAcceptable = "Not Acceptable";
    /// <summary>The conventional reason phrase for status 407.</summary>
    public const string ProxyAuthenticationRequired = "Proxy Authentication Required";
    /// <summary>The conventional reason phrase for status 408.</summary>
    public const string RequestTimeout = "Request Timeout";
    /// <summary>The conventional reason phrase for status 409.</summary>
    public const string Conflict = "Conflict";
    /// <summary>The conventional reason phrase for status 410.</summary>
    public const string Gone = "Gone";
    /// <summary>The conventional reason phrase for status 411.</summary>
    public const string LengthRequired = "Length Required";
    /// <summary>The conventional reason phrase for status 412.</summary>
    public const string PreconditionFailed = "Precondition Failed";
    /// <summary>The conventional reason phrase for status 413.</summary>
    public const string ContentTooLarge = "Content Too Large";
    /// <summary>The conventional reason phrase for status 414.</summary>
    public const string UriTooLong = "URI Too Long";
    /// <summary>The conventional reason phrase for status 415.</summary>
    public const string UnsupportedMediaType = "Unsupported Media Type";
    /// <summary>The conventional reason phrase for status 416.</summary>
    public const string RangeNotSatisfiable = "Range Not Satisfiable";
    /// <summary>The conventional reason phrase for status 417.</summary>
    public const string ExpectationFailed = "Expectation Failed";
    /// <summary>The conventional reason phrase for unused status 418.</summary>
    public const string Unused418 = "(Unused)";
    /// <summary>The conventional reason phrase for status 421.</summary>
    public const string MisdirectedRequest = "Misdirected Request";
    /// <summary>The conventional reason phrase for status 422.</summary>
    public const string UnprocessableContent = "Unprocessable Content";
    /// <summary>The conventional reason phrase for status 423.</summary>
    public const string Locked = "Locked";
    /// <summary>The conventional reason phrase for status 424.</summary>
    public const string FailedDependency = "Failed Dependency";
    /// <summary>The conventional reason phrase for status 425.</summary>
    public const string TooEarly = "Too Early";
    /// <summary>The conventional reason phrase for status 426.</summary>
    public const string UpgradeRequired = "Upgrade Required";
    /// <summary>The conventional reason phrase for status 428.</summary>
    public const string PreconditionRequired = "Precondition Required";
    /// <summary>The conventional reason phrase for status 429.</summary>
    public const string TooManyRequests = "Too Many Requests";
    /// <summary>The conventional reason phrase for status 431.</summary>
    public const string RequestHeaderFieldsTooLarge = "Request Header Fields Too Large";
    /// <summary>The conventional reason phrase for status 451.</summary>
    public const string UnavailableForLegalReasons = "Unavailable For Legal Reasons";
    /// <summary>The conventional reason phrase for status 500.</summary>
    public const string InternalServerError = "Internal Server Error";
    /// <summary>The conventional reason phrase for status 501.</summary>
    public const string NotImplemented = "Not Implemented";
    /// <summary>The conventional reason phrase for status 502.</summary>
    public const string BadGateway = "Bad Gateway";
    /// <summary>The conventional reason phrase for status 503.</summary>
    public const string ServiceUnavailable = "Service Unavailable";
    /// <summary>The conventional reason phrase for status 504.</summary>
    public const string GatewayTimeout = "Gateway Timeout";
    /// <summary>The conventional reason phrase for status 505.</summary>
    public const string HttpVersionNotSupported = "HTTP Version Not Supported";
    /// <summary>The conventional reason phrase for status 506.</summary>
    public const string VariantAlsoNegotiates = "Variant Also Negotiates";
    /// <summary>The conventional reason phrase for status 507.</summary>
    public const string InsufficientStorage = "Insufficient Storage";
    /// <summary>The conventional reason phrase for status 508.</summary>
    public const string LoopDetected = "Loop Detected";
    /// <summary>The conventional reason phrase for status 510.</summary>
    public const string NotExtended = "Not Extended";
    /// <summary>The conventional reason phrase for status 511.</summary>
    public const string NetworkAuthenticationRequired = "Network Authentication Required";
}
