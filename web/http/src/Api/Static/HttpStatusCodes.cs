namespace http;

/// <summary>
/// Provides status codes from the IANA HTTP Status Code Registry snapshot.
/// </summary>
public static class HttpStatusCodes
{
    /// <summary>
    /// The registered <c>100</c> Continue status code.
    /// </summary>
    public const int Continue = 100;

    /// <summary>
    /// The registered <c>101</c> Switching Protocols status code.
    /// </summary>
    public const int SwitchingProtocols = 101;

    /// <summary>
    /// The registered <c>102</c> Processing status code.
    /// </summary>
    public const int Processing = 102;

    /// <summary>
    /// The registered <c>103</c> Early Hints status code.
    /// </summary>
    public const int EarlyHints = 103;

    /// <summary>
    /// The registered <c>104</c> Upload Resumption Supported (TEMPORARY - registered 2024-11-13, extension registered 2025-09-15, expires 2026-11-13) status code.
    /// </summary>
    public const int UploadResumptionSupported = 104;

    /// <summary>
    /// The registered <c>200</c> OK status code.
    /// </summary>
    public const int Ok = 200;

    /// <summary>
    /// The registered <c>201</c> Created status code.
    /// </summary>
    public const int Created = 201;

    /// <summary>
    /// The registered <c>202</c> Accepted status code.
    /// </summary>
    public const int Accepted = 202;

    /// <summary>
    /// The registered <c>203</c> Non-Authoritative Information status code.
    /// </summary>
    public const int NonAuthoritativeInformation = 203;

    /// <summary>
    /// The registered <c>204</c> No Content status code.
    /// </summary>
    public const int NoContent = 204;

    /// <summary>
    /// The registered <c>205</c> Reset Content status code.
    /// </summary>
    public const int ResetContent = 205;

    /// <summary>
    /// The registered <c>206</c> Partial Content status code.
    /// </summary>
    public const int PartialContent = 206;

    /// <summary>
    /// The registered <c>207</c> Multi-Status status code.
    /// </summary>
    public const int MultiStatus = 207;

    /// <summary>
    /// The registered <c>208</c> Already Reported status code.
    /// </summary>
    public const int AlreadyReported = 208;

    /// <summary>
    /// The registered <c>226</c> IM Used status code.
    /// </summary>
    public const int ImUsed = 226;

    /// <summary>
    /// The registered <c>300</c> Multiple Choices status code.
    /// </summary>
    public const int MultipleChoices = 300;

    /// <summary>
    /// The registered <c>301</c> Moved Permanently status code.
    /// </summary>
    public const int MovedPermanently = 301;

    /// <summary>
    /// The registered <c>302</c> Found status code.
    /// </summary>
    public const int Found = 302;

    /// <summary>
    /// The registered <c>303</c> See Other status code.
    /// </summary>
    public const int SeeOther = 303;

    /// <summary>
    /// The registered <c>304</c> Not Modified status code.
    /// </summary>
    public const int NotModified = 304;

    /// <summary>
    /// The registered <c>305</c> Use Proxy status code.
    /// </summary>
    public const int UseProxy = 305;

    /// <summary>
    /// The registered <c>306</c> (Unused) status code.
    /// </summary>
    public const int Unused306 = 306;

    /// <summary>
    /// The registered <c>307</c> Temporary Redirect status code.
    /// </summary>
    public const int TemporaryRedirect = 307;

    /// <summary>
    /// The registered <c>308</c> Permanent Redirect status code.
    /// </summary>
    public const int PermanentRedirect = 308;

    /// <summary>
    /// The registered <c>400</c> Bad Request status code.
    /// </summary>
    public const int BadRequest = 400;

    /// <summary>
    /// The registered <c>401</c> Unauthorized status code.
    /// </summary>
    public const int Unauthorized = 401;

    /// <summary>
    /// The registered <c>402</c> Payment Required status code.
    /// </summary>
    public const int PaymentRequired = 402;

    /// <summary>
    /// The registered <c>403</c> Forbidden status code.
    /// </summary>
    public const int Forbidden = 403;

    /// <summary>
    /// The registered <c>404</c> Not Found status code.
    /// </summary>
    public const int NotFound = 404;

    /// <summary>
    /// The registered <c>405</c> Method Not Allowed status code.
    /// </summary>
    public const int MethodNotAllowed = 405;

    /// <summary>
    /// The registered <c>406</c> Not Acceptable status code.
    /// </summary>
    public const int NotAcceptable = 406;

    /// <summary>
    /// The registered <c>407</c> Proxy Authentication Required status code.
    /// </summary>
    public const int ProxyAuthenticationRequired = 407;

    /// <summary>
    /// The registered <c>408</c> Request Timeout status code.
    /// </summary>
    public const int RequestTimeout = 408;

    /// <summary>
    /// The registered <c>409</c> Conflict status code.
    /// </summary>
    public const int Conflict = 409;

    /// <summary>
    /// The registered <c>410</c> Gone status code.
    /// </summary>
    public const int Gone = 410;

    /// <summary>
    /// The registered <c>411</c> Length Required status code.
    /// </summary>
    public const int LengthRequired = 411;

    /// <summary>
    /// The registered <c>412</c> Precondition Failed status code.
    /// </summary>
    public const int PreconditionFailed = 412;

    /// <summary>
    /// The registered <c>413</c> Content Too Large status code.
    /// </summary>
    public const int ContentTooLarge = 413;

    /// <summary>
    /// The registered <c>414</c> URI Too Long status code.
    /// </summary>
    public const int UriTooLong = 414;

    /// <summary>
    /// The registered <c>415</c> Unsupported Media Type status code.
    /// </summary>
    public const int UnsupportedMediaType = 415;

    /// <summary>
    /// The registered <c>416</c> Range Not Satisfiable status code.
    /// </summary>
    public const int RangeNotSatisfiable = 416;

    /// <summary>
    /// The registered <c>417</c> Expectation Failed status code.
    /// </summary>
    public const int ExpectationFailed = 417;

    /// <summary>
    /// The registered <c>418</c> (Unused) status code.
    /// </summary>
    public const int Unused418 = 418;

    /// <summary>
    /// The registered <c>421</c> Misdirected Request status code.
    /// </summary>
    public const int MisdirectedRequest = 421;

    /// <summary>
    /// The registered <c>422</c> Unprocessable Content status code.
    /// </summary>
    public const int UnprocessableContent = 422;

    /// <summary>
    /// The registered <c>423</c> Locked status code.
    /// </summary>
    public const int Locked = 423;

    /// <summary>
    /// The registered <c>424</c> Failed Dependency status code.
    /// </summary>
    public const int FailedDependency = 424;

    /// <summary>
    /// The registered <c>425</c> Too Early status code.
    /// </summary>
    public const int TooEarly = 425;

    /// <summary>
    /// The registered <c>426</c> Upgrade Required status code.
    /// </summary>
    public const int UpgradeRequired = 426;

    /// <summary>
    /// The registered <c>428</c> Precondition Required status code.
    /// </summary>
    public const int PreconditionRequired = 428;

    /// <summary>
    /// The registered <c>429</c> Too Many Requests status code.
    /// </summary>
    public const int TooManyRequests = 429;

    /// <summary>
    /// The registered <c>431</c> Request Header Fields Too Large status code.
    /// </summary>
    public const int RequestHeaderFieldsTooLarge = 431;

    /// <summary>
    /// The registered <c>451</c> Unavailable For Legal Reasons status code.
    /// </summary>
    public const int UnavailableForLegalReasons = 451;

    /// <summary>
    /// The registered <c>500</c> Internal Server Error status code.
    /// </summary>
    public const int InternalServerError = 500;

    /// <summary>
    /// The registered <c>501</c> Not Implemented status code.
    /// </summary>
    public const int NotImplemented = 501;

    /// <summary>
    /// The registered <c>502</c> Bad Gateway status code.
    /// </summary>
    public const int BadGateway = 502;

    /// <summary>
    /// The registered <c>503</c> Service Unavailable status code.
    /// </summary>
    public const int ServiceUnavailable = 503;

    /// <summary>
    /// The registered <c>504</c> Gateway Timeout status code.
    /// </summary>
    public const int GatewayTimeout = 504;

    /// <summary>
    /// The registered <c>505</c> HTTP Version Not Supported status code.
    /// </summary>
    public const int HttpVersionNotSupported = 505;

    /// <summary>
    /// The registered <c>506</c> Variant Also Negotiates status code.
    /// </summary>
    public const int VariantAlsoNegotiates = 506;

    /// <summary>
    /// The registered <c>507</c> Insufficient Storage status code.
    /// </summary>
    public const int InsufficientStorage = 507;

    /// <summary>
    /// The registered <c>508</c> Loop Detected status code.
    /// </summary>
    public const int LoopDetected = 508;

    /// <summary>
    /// The registered <c>510</c> Not Extended (OBSOLETED) status code.
    /// </summary>
    public const int NotExtended = 510;

    /// <summary>
    /// The registered <c>511</c> Network Authentication Required status code.
    /// </summary>
    public const int NetworkAuthenticationRequired = 511;
}
