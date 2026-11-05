namespace sip;

/// <summary>
/// Provides conventional, non-normative reason phrases for registered SIP status codes.
/// </summary>
public static class SipReasonPhrases
{
    /// <summary>The conventional phrase for status 100.</summary>
    public const string Trying = "Trying";

    /// <summary>The conventional phrase for status 180.</summary>
    public const string Ringing = "Ringing";

    /// <summary>The conventional phrase for status 181.</summary>
    public const string CallIsBeingForwarded = "Call Is Being Forwarded";

    /// <summary>The conventional phrase for status 182.</summary>
    public const string Queued = "Queued";

    /// <summary>The conventional phrase for status 183.</summary>
    public const string SessionProgress = "Session Progress";

    /// <summary>The conventional phrase for status 199.</summary>
    public const string EarlyDialogTerminated = "Early Dialog Terminated";

    /// <summary>The conventional phrase for status 200.</summary>
    public const string Ok = "OK";

    /// <summary>The conventional phrase for status 202.</summary>
    public const string Accepted = "Accepted";

    /// <summary>The conventional phrase for status 204.</summary>
    public const string NoNotification = "No Notification";

    /// <summary>The conventional phrase for status 300.</summary>
    public const string MultipleChoices = "Multiple Choices";

    /// <summary>The conventional phrase for status 301.</summary>
    public const string MovedPermanently = "Moved Permanently";

    /// <summary>The conventional phrase for status 302.</summary>
    public const string MovedTemporarily = "Moved Temporarily";

    /// <summary>The conventional phrase for status 305.</summary>
    public const string UseProxy = "Use Proxy";

    /// <summary>The conventional phrase for status 380.</summary>
    public const string AlternativeService = "Alternative Service";

    /// <summary>The conventional phrase for status 400.</summary>
    public const string BadRequest = "Bad Request";

    /// <summary>The conventional phrase for status 401.</summary>
    public const string Unauthorized = "Unauthorized";

    /// <summary>The conventional phrase for status 402.</summary>
    public const string PaymentRequired = "Payment Required";

    /// <summary>The conventional phrase for status 403.</summary>
    public const string Forbidden = "Forbidden";

    /// <summary>The conventional phrase for status 404.</summary>
    public const string NotFound = "Not Found";

    /// <summary>The conventional phrase for status 405.</summary>
    public const string MethodNotAllowed = "Method Not Allowed";

    /// <summary>The conventional phrase for status 406.</summary>
    public const string NotAcceptable = "Not Acceptable";

    /// <summary>The conventional phrase for status 407.</summary>
    public const string ProxyAuthenticationRequired = "Proxy Authentication Required";

    /// <summary>The conventional phrase for status 408.</summary>
    public const string RequestTimeout = "Request Timeout";

    /// <summary>The conventional phrase for status 410.</summary>
    public const string Gone = "Gone";

    /// <summary>The conventional phrase for status 412.</summary>
    public const string ConditionalRequestFailed = "Conditional Request Failed";

    /// <summary>The conventional phrase for status 413.</summary>
    public const string RequestEntityTooLarge = "Request Entity Too Large";

    /// <summary>The conventional phrase for status 414.</summary>
    public const string RequestUriTooLong = "Request-URI Too Long";

    /// <summary>The conventional phrase for status 415.</summary>
    public const string UnsupportedMediaType = "Unsupported Media Type";

    /// <summary>The conventional phrase for status 416.</summary>
    public const string UnsupportedUriScheme = "Unsupported URI Scheme";

    /// <summary>The conventional phrase for status 417.</summary>
    public const string UnknownResourcePriority = "Unknown Resource-Priority";

    /// <summary>The conventional phrase for status 420.</summary>
    public const string BadExtension = "Bad Extension";

    /// <summary>The conventional phrase for status 421.</summary>
    public const string ExtensionRequired = "Extension Required";

    /// <summary>The conventional phrase for status 422.</summary>
    public const string SessionIntervalTooSmall = "Session Interval Too Small";

    /// <summary>The conventional phrase for status 423.</summary>
    public const string IntervalTooBrief = "Interval Too Brief";

    /// <summary>The conventional phrase for status 424.</summary>
    public const string BadLocationInformation = "Bad Location Information";

    /// <summary>The conventional phrase for status 425.</summary>
    public const string BadAlertMessage = "Bad Alert Message";

    /// <summary>The conventional phrase for status 428.</summary>
    public const string UseIdentityHeader = "Use Identity Header";

    /// <summary>The conventional phrase for status 429.</summary>
    public const string ProvideReferrerIdentity = "Provide Referrer Identity";

    /// <summary>The conventional phrase for status 430.</summary>
    public const string FlowFailed = "Flow Failed";

    /// <summary>The conventional phrase for status 433.</summary>
    public const string AnonymityDisallowed = "Anonymity Disallowed";

    /// <summary>The conventional phrase for status 436.</summary>
    public const string BadIdentityInfo = "Bad Identity-Info";

    /// <summary>The conventional phrase for status 437.</summary>
    public const string UnsupportedCertificate = "Unsupported Certificate";

    /// <summary>The conventional phrase for status 438.</summary>
    public const string InvalidIdentityHeader = "Invalid Identity Header";

    /// <summary>The conventional phrase for status 439.</summary>
    public const string FirstHopLacksOutboundSupport = "First Hop Lacks Outbound Support";

    /// <summary>The conventional phrase for status 440.</summary>
    public const string MaxBreadthExceeded = "Max-Breadth Exceeded";

    /// <summary>The conventional phrase for status 469.</summary>
    public const string BadInfoPackage = "Bad Info Package";

    /// <summary>The conventional phrase for status 470.</summary>
    public const string ConsentNeeded = "Consent Needed";

    /// <summary>The conventional phrase for status 480.</summary>
    public const string TemporarilyUnavailable = "Temporarily Unavailable";

    /// <summary>The conventional phrase for status 481.</summary>
    public const string CallOrTransactionDoesNotExist = "Call/Transaction Does Not Exist";

    /// <summary>The conventional phrase for status 482.</summary>
    public const string LoopDetected = "Loop Detected";

    /// <summary>The conventional phrase for status 483.</summary>
    public const string TooManyHops = "Too Many Hops";

    /// <summary>The conventional phrase for status 484.</summary>
    public const string AddressIncomplete = "Address Incomplete";

    /// <summary>The conventional phrase for status 485.</summary>
    public const string Ambiguous = "Ambiguous";

    /// <summary>The conventional phrase for status 486.</summary>
    public const string BusyHere = "Busy Here";

    /// <summary>The conventional phrase for status 487.</summary>
    public const string RequestTerminated = "Request Terminated";

    /// <summary>The conventional phrase for status 488.</summary>
    public const string NotAcceptableHere = "Not Acceptable Here";

    /// <summary>The conventional phrase for status 489.</summary>
    public const string BadEvent = "Bad Event";

    /// <summary>The conventional phrase for status 491.</summary>
    public const string RequestPending = "Request Pending";

    /// <summary>The conventional phrase for status 493.</summary>
    public const string Undecipherable = "Undecipherable";

    /// <summary>The conventional phrase for status 494.</summary>
    public const string SecurityAgreementRequired = "Security Agreement Required";

    /// <summary>The conventional phrase for status 500.</summary>
    public const string ServerInternalError = "Server Internal Error";

    /// <summary>The conventional phrase for status 501.</summary>
    public const string NotImplemented = "Not Implemented";

    /// <summary>The conventional phrase for status 502.</summary>
    public const string BadGateway = "Bad Gateway";

    /// <summary>The conventional phrase for status 503.</summary>
    public const string ServiceUnavailable = "Service Unavailable";

    /// <summary>The conventional phrase for status 504.</summary>
    public const string ServerTimeout = "Server Time-out";

    /// <summary>The conventional phrase for status 505.</summary>
    public const string VersionNotSupported = "Version Not Supported";

    /// <summary>The conventional phrase for status 513.</summary>
    public const string MessageTooLarge = "Message Too Large";

    /// <summary>The conventional phrase for status 555.</summary>
    public const string PushNotificationServiceNotSupported = "Push Notification Service Not Supported";

    /// <summary>The conventional phrase for status 580.</summary>
    public const string PreconditionFailure = "Precondition Failure";

    /// <summary>The conventional phrase for status 600.</summary>
    public const string BusyEverywhere = "Busy Everywhere";

    /// <summary>The conventional phrase for status 603.</summary>
    public const string Decline = "Decline";

    /// <summary>The conventional phrase for status 604.</summary>
    public const string DoesNotExistAnywhere = "Does Not Exist Anywhere";

    /// <summary>The conventional phrase for status 606.</summary>
    public const string GloballyNotAcceptable = "Not Acceptable";

    /// <summary>The conventional phrase for status 607.</summary>
    public const string Unwanted = "Unwanted";

    /// <summary>The conventional phrase for status 608.</summary>
    public const string Rejected = "Rejected";
}
