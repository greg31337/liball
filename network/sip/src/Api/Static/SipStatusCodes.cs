namespace sip;

/// <summary>
/// Provides registered core and extension SIP response status codes.
/// </summary>
public static class SipStatusCodes
{
    /// <summary>The Trying status code.</summary>
    public const int Trying = 100;

    /// <summary>The Ringing status code.</summary>
    public const int Ringing = 180;

    /// <summary>The Call Is Being Forwarded status code.</summary>
    public const int CallIsBeingForwarded = 181;

    /// <summary>The Queued status code.</summary>
    public const int Queued = 182;

    /// <summary>The Session Progress status code.</summary>
    public const int SessionProgress = 183;

    /// <summary>The Early Dialog Terminated status code.</summary>
    public const int EarlyDialogTerminated = 199;

    /// <summary>The OK status code.</summary>
    public const int Ok = 200;

    /// <summary>The Accepted status code.</summary>
    public const int Accepted = 202;

    /// <summary>The No Notification status code.</summary>
    public const int NoNotification = 204;

    /// <summary>The Multiple Choices status code.</summary>
    public const int MultipleChoices = 300;

    /// <summary>The Moved Permanently status code.</summary>
    public const int MovedPermanently = 301;

    /// <summary>The Moved Temporarily status code.</summary>
    public const int MovedTemporarily = 302;

    /// <summary>The Use Proxy status code.</summary>
    public const int UseProxy = 305;

    /// <summary>The Alternative Service status code.</summary>
    public const int AlternativeService = 380;

    /// <summary>The Bad Request status code.</summary>
    public const int BadRequest = 400;

    /// <summary>The Unauthorized status code.</summary>
    public const int Unauthorized = 401;

    /// <summary>The Payment Required status code.</summary>
    public const int PaymentRequired = 402;

    /// <summary>The Forbidden status code.</summary>
    public const int Forbidden = 403;

    /// <summary>The Not Found status code.</summary>
    public const int NotFound = 404;

    /// <summary>The Method Not Allowed status code.</summary>
    public const int MethodNotAllowed = 405;

    /// <summary>The Not Acceptable status code.</summary>
    public const int NotAcceptable = 406;

    /// <summary>The Proxy Authentication Required status code.</summary>
    public const int ProxyAuthenticationRequired = 407;

    /// <summary>The Request Timeout status code.</summary>
    public const int RequestTimeout = 408;

    /// <summary>The Conflict status code.</summary>
    public const int Conflict = 409;

    /// <summary>The Gone status code.</summary>
    public const int Gone = 410;

    /// <summary>The Conditional Request Failed status code.</summary>
    public const int ConditionalRequestFailed = 412;

    /// <summary>The Request Entity Too Large status code.</summary>
    public const int RequestEntityTooLarge = 413;

    /// <summary>The Request-URI Too Long status code.</summary>
    public const int RequestUriTooLong = 414;

    /// <summary>The Unsupported Media Type status code.</summary>
    public const int UnsupportedMediaType = 415;

    /// <summary>The Unsupported URI Scheme status code.</summary>
    public const int UnsupportedUriScheme = 416;

    /// <summary>The Unknown Resource-Priority status code.</summary>
    public const int UnknownResourcePriority = 417;

    /// <summary>The Bad Extension status code.</summary>
    public const int BadExtension = 420;

    /// <summary>The Extension Required status code.</summary>
    public const int ExtensionRequired = 421;

    /// <summary>The Session Interval Too Small status code.</summary>
    public const int SessionIntervalTooSmall = 422;

    /// <summary>The Interval Too Brief status code.</summary>
    public const int IntervalTooBrief = 423;

    /// <summary>The Bad Location Information status code.</summary>
    public const int BadLocationInformation = 424;

    /// <summary>The Bad Alert Message status code.</summary>
    public const int BadAlertMessage = 425;

    /// <summary>The Use Identity Header status code.</summary>
    public const int UseIdentityHeader = 428;

    /// <summary>The Provide Referrer Identity status code.</summary>
    public const int ProvideReferrerIdentity = 429;

    /// <summary>The Flow Failed status code.</summary>
    public const int FlowFailed = 430;

    /// <summary>The Anonymity Disallowed status code.</summary>
    public const int AnonymityDisallowed = 433;

    /// <summary>The Bad Identity-Info status code.</summary>
    public const int BadIdentityInfo = 436;

    /// <summary>The Unsupported Certificate status code.</summary>
    public const int UnsupportedCertificate = 437;

    /// <summary>The Invalid Identity Header status code.</summary>
    public const int InvalidIdentityHeader = 438;

    /// <summary>The First Hop Lacks Outbound Support status code.</summary>
    public const int FirstHopLacksOutboundSupport = 439;

    /// <summary>The Max-Breadth Exceeded status code.</summary>
    public const int MaxBreadthExceeded = 440;

    /// <summary>The Bad Info Package status code.</summary>
    public const int BadInfoPackage = 469;

    /// <summary>The Consent Needed status code.</summary>
    public const int ConsentNeeded = 470;

    /// <summary>The Temporarily Unavailable status code.</summary>
    public const int TemporarilyUnavailable = 480;

    /// <summary>The Call or Transaction Does Not Exist status code.</summary>
    public const int CallOrTransactionDoesNotExist = 481;

    /// <summary>The Loop Detected status code.</summary>
    public const int LoopDetected = 482;

    /// <summary>The Too Many Hops status code.</summary>
    public const int TooManyHops = 483;

    /// <summary>The Address Incomplete status code.</summary>
    public const int AddressIncomplete = 484;

    /// <summary>The Ambiguous status code.</summary>
    public const int Ambiguous = 485;

    /// <summary>The Busy Here status code.</summary>
    public const int BusyHere = 486;

    /// <summary>The Request Terminated status code.</summary>
    public const int RequestTerminated = 487;

    /// <summary>The Not Acceptable Here status code.</summary>
    public const int NotAcceptableHere = 488;

    /// <summary>The Bad Event status code.</summary>
    public const int BadEvent = 489;

    /// <summary>The Request Pending status code.</summary>
    public const int RequestPending = 491;

    /// <summary>The Undecipherable status code.</summary>
    public const int Undecipherable = 493;

    /// <summary>The Security Agreement Required status code.</summary>
    public const int SecurityAgreementRequired = 494;

    /// <summary>The Server Internal Error status code.</summary>
    public const int ServerInternalError = 500;

    /// <summary>The Not Implemented status code.</summary>
    public const int NotImplemented = 501;

    /// <summary>The Bad Gateway status code.</summary>
    public const int BadGateway = 502;

    /// <summary>The Service Unavailable status code.</summary>
    public const int ServiceUnavailable = 503;

    /// <summary>The Server Time-out status code.</summary>
    public const int ServerTimeout = 504;

    /// <summary>The Version Not Supported status code.</summary>
    public const int VersionNotSupported = 505;

    /// <summary>The Message Too Large status code.</summary>
    public const int MessageTooLarge = 513;

    /// <summary>The Push Notification Service Not Supported status code.</summary>
    public const int PushNotificationServiceNotSupported = 555;

    /// <summary>The Precondition Failure status code.</summary>
    public const int PreconditionFailure = 580;

    /// <summary>The Busy Everywhere status code.</summary>
    public const int BusyEverywhere = 600;

    /// <summary>The Decline status code.</summary>
    public const int Decline = 603;

    /// <summary>The Does Not Exist Anywhere status code.</summary>
    public const int DoesNotExistAnywhere = 604;

    /// <summary>The Not Acceptable global failure status code.</summary>
    public const int GloballyNotAcceptable = 606;

    /// <summary>The Unwanted status code.</summary>
    public const int Unwanted = 607;

    /// <summary>The Rejected status code.</summary>
    public const int Rejected = 608;
}
