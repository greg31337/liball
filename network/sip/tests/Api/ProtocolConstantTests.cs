namespace sip.Tests;

public class ProtocolConstantTests
{
    [Test]
    public void MethodsMatchWireTokens()
    {
        string[] actual =
        [
            SipMethods.Ack,
            SipMethods.Bye,
            SipMethods.Cancel,
            SipMethods.Info,
            SipMethods.Invite,
            SipMethods.Message,
            SipMethods.Notify,
            SipMethods.Options,
            SipMethods.Prack,
            SipMethods.Publish,
            SipMethods.Refer,
            SipMethods.Register,
            SipMethods.Subscribe,
            SipMethods.Update
        ];

        string[] expected =
        [
            "ACK", "BYE", "CANCEL", "INFO", "INVITE", "MESSAGE", "NOTIFY",
            "OPTIONS", "PRACK", "PUBLISH", "REFER", "REGISTER", "SUBSCRIBE", "UPDATE"
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void HeaderNamesMatchCanonicalAndCompactWireNames()
    {
        string[] actual =
        [
            SipHeaderNames.Accept,
            SipHeaderNames.AcceptContact,
            SipHeaderNames.AcceptEncoding,
            SipHeaderNames.AcceptLanguage,
            SipHeaderNames.AcceptResourcePriority,
            SipHeaderNames.AlertInfo,
            SipHeaderNames.Allow,
            SipHeaderNames.AllowEvents,
            SipHeaderNames.AnswerMode,
            SipHeaderNames.AuthenticationInfo,
            SipHeaderNames.Authorization,
            SipHeaderNames.CallId,
            SipHeaderNames.CallInfo,
            SipHeaderNames.Contact,
            SipHeaderNames.ContentDisposition,
            SipHeaderNames.ContentEncoding,
            SipHeaderNames.ContentLanguage,
            SipHeaderNames.ContentLength,
            SipHeaderNames.ContentType,
            SipHeaderNames.CSeq,
            SipHeaderNames.Date,
            SipHeaderNames.ErrorInfo,
            SipHeaderNames.Event,
            SipHeaderNames.Expires,
            SipHeaderNames.FeatureCaps,
            SipHeaderNames.FlowTimer,
            SipHeaderNames.From,
            SipHeaderNames.Geolocation,
            SipHeaderNames.GeolocationError,
            SipHeaderNames.GeolocationRouting,
            SipHeaderNames.HistoryInfo,
            SipHeaderNames.Identity,
            SipHeaderNames.IdentityInfo,
            SipHeaderNames.InfoPackage,
            SipHeaderNames.InReplyTo,
            SipHeaderNames.Join,
            SipHeaderNames.MaxBreadth,
            SipHeaderNames.MaxForwards,
            SipHeaderNames.MimeVersion,
            SipHeaderNames.MinExpires,
            SipHeaderNames.MinSE,
            SipHeaderNames.Organization,
            SipHeaderNames.PAccessNetworkInfo,
            SipHeaderNames.PAssertedIdentity,
            SipHeaderNames.PAssociatedUri,
            SipHeaderNames.PCalledPartyId,
            SipHeaderNames.PChargingFunctionAddresses,
            SipHeaderNames.PChargingVector,
            SipHeaderNames.PEarlyMedia,
            SipHeaderNames.PMediaAuthorization,
            SipHeaderNames.PPreferredIdentity,
            SipHeaderNames.Path,
            SipHeaderNames.PermissionMissing,
            SipHeaderNames.Priority,
            SipHeaderNames.PrivateAnswerMode,
            SipHeaderNames.Privacy,
            SipHeaderNames.ProxyAuthenticate,
            SipHeaderNames.ProxyAuthorization,
            SipHeaderNames.ProxyRequire,
            SipHeaderNames.RAck,
            SipHeaderNames.Reason,
            SipHeaderNames.RecordRoute,
            SipHeaderNames.ReceiveInfo,
            SipHeaderNames.ReferSub,
            SipHeaderNames.ReferTo,
            SipHeaderNames.ReferredBy,
            SipHeaderNames.RejectContact,
            SipHeaderNames.Replaces,
            SipHeaderNames.ReplyTo,
            SipHeaderNames.RequestDisposition,
            SipHeaderNames.Require,
            SipHeaderNames.ResourcePriority,
            SipHeaderNames.RetryAfter,
            SipHeaderNames.Route,
            SipHeaderNames.RSeq,
            SipHeaderNames.SecurityClient,
            SipHeaderNames.SecurityServer,
            SipHeaderNames.SecurityVerify,
            SipHeaderNames.Server,
            SipHeaderNames.ServiceRoute,
            SipHeaderNames.SessionExpires,
            SipHeaderNames.SessionId,
            SipHeaderNames.SipETag,
            SipHeaderNames.SipIfMatch,
            SipHeaderNames.Subject,
            SipHeaderNames.SubscriptionState,
            SipHeaderNames.Supported,
            SipHeaderNames.TargetDialog,
            SipHeaderNames.Timestamp,
            SipHeaderNames.To,
            SipHeaderNames.TriggerConsent,
            SipHeaderNames.Unsupported,
            SipHeaderNames.UserAgent,
            SipHeaderNames.UserToUser,
            SipHeaderNames.Via,
            SipHeaderNames.Warning,
            SipHeaderNames.WwwAuthenticate,
            SipHeaderNames.CompactAcceptContact,
            SipHeaderNames.CompactAllowEvents,
            SipHeaderNames.CompactCallId,
            SipHeaderNames.CompactContact,
            SipHeaderNames.CompactContentEncoding,
            SipHeaderNames.CompactContentLength,
            SipHeaderNames.CompactContentType,
            SipHeaderNames.CompactEvent,
            SipHeaderNames.CompactFrom,
            SipHeaderNames.CompactIdentity,
            SipHeaderNames.CompactReferTo,
            SipHeaderNames.CompactReferredBy,
            SipHeaderNames.CompactRejectContact,
            SipHeaderNames.CompactRequestDisposition,
            SipHeaderNames.CompactSessionExpires,
            SipHeaderNames.CompactSubject,
            SipHeaderNames.CompactSupported,
            SipHeaderNames.CompactTo,
            SipHeaderNames.CompactVia
        ];

        string[] expected =
        [
            "Accept", "Accept-Contact", "Accept-Encoding", "Accept-Language",
            "Accept-Resource-Priority", "Alert-Info", "Allow", "Allow-Events", "Answer-Mode",
            "Authentication-Info", "Authorization", "Call-ID", "Call-Info", "Contact",
            "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length",
            "Content-Type", "CSeq", "Date", "Error-Info", "Event", "Expires", "Feature-Caps",
            "Flow-Timer", "From", "Geolocation", "Geolocation-Error", "Geolocation-Routing",
            "History-Info", "Identity", "Identity-Info", "Info-Package", "In-Reply-To", "Join",
            "Max-Breadth", "Max-Forwards", "MIME-Version", "Min-Expires", "Min-SE", "Organization",
            "P-Access-Network-Info", "P-Asserted-Identity", "P-Associated-URI", "P-Called-Party-ID",
            "P-Charging-Function-Addresses", "P-Charging-Vector", "P-Early-Media",
            "P-Media-Authorization", "P-Preferred-Identity", "Path", "Permission-Missing", "Priority",
            "Priv-Answer-Mode", "Privacy", "Proxy-Authenticate", "Proxy-Authorization", "Proxy-Require",
            "RAck", "Reason", "Record-Route", "Recv-Info", "Refer-Sub", "Refer-To", "Referred-By",
            "Reject-Contact", "Replaces", "Reply-To", "Request-Disposition", "Require",
            "Resource-Priority", "Retry-After", "Route", "RSeq", "Security-Client", "Security-Server",
            "Security-Verify", "Server", "Service-Route", "Session-Expires", "Session-ID", "SIP-ETag",
            "SIP-If-Match", "Subject", "Subscription-State", "Supported", "Target-Dialog", "Timestamp",
            "To", "Trigger-Consent", "Unsupported", "User-Agent", "User-to-User", "Via", "Warning",
            "WWW-Authenticate", "a", "u", "i", "m", "e", "l", "c", "o", "f", "y", "r", "b",
            "j", "d", "x", "s", "k", "t", "v"
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void StatusCodesMatchRegisteredNumbers()
    {
        int[] actual =
        [
            SipStatusCodes.Trying,
            SipStatusCodes.Ringing,
            SipStatusCodes.CallIsBeingForwarded,
            SipStatusCodes.Queued,
            SipStatusCodes.SessionProgress,
            SipStatusCodes.EarlyDialogTerminated,
            SipStatusCodes.Ok,
            SipStatusCodes.Accepted,
            SipStatusCodes.NoNotification,
            SipStatusCodes.MultipleChoices,
            SipStatusCodes.MovedPermanently,
            SipStatusCodes.MovedTemporarily,
            SipStatusCodes.UseProxy,
            SipStatusCodes.AlternativeService,
            SipStatusCodes.BadRequest,
            SipStatusCodes.Unauthorized,
            SipStatusCodes.PaymentRequired,
            SipStatusCodes.Forbidden,
            SipStatusCodes.NotFound,
            SipStatusCodes.MethodNotAllowed,
            SipStatusCodes.NotAcceptable,
            SipStatusCodes.ProxyAuthenticationRequired,
            SipStatusCodes.RequestTimeout,
            SipStatusCodes.Gone,
            SipStatusCodes.ConditionalRequestFailed,
            SipStatusCodes.RequestEntityTooLarge,
            SipStatusCodes.RequestUriTooLong,
            SipStatusCodes.UnsupportedMediaType,
            SipStatusCodes.UnsupportedUriScheme,
            SipStatusCodes.UnknownResourcePriority,
            SipStatusCodes.BadExtension,
            SipStatusCodes.ExtensionRequired,
            SipStatusCodes.SessionIntervalTooSmall,
            SipStatusCodes.IntervalTooBrief,
            SipStatusCodes.BadLocationInformation,
            SipStatusCodes.BadAlertMessage,
            SipStatusCodes.UseIdentityHeader,
            SipStatusCodes.ProvideReferrerIdentity,
            SipStatusCodes.FlowFailed,
            SipStatusCodes.AnonymityDisallowed,
            SipStatusCodes.BadIdentityInfo,
            SipStatusCodes.UnsupportedCertificate,
            SipStatusCodes.InvalidIdentityHeader,
            SipStatusCodes.FirstHopLacksOutboundSupport,
            SipStatusCodes.MaxBreadthExceeded,
            SipStatusCodes.BadInfoPackage,
            SipStatusCodes.ConsentNeeded,
            SipStatusCodes.TemporarilyUnavailable,
            SipStatusCodes.CallOrTransactionDoesNotExist,
            SipStatusCodes.LoopDetected,
            SipStatusCodes.TooManyHops,
            SipStatusCodes.AddressIncomplete,
            SipStatusCodes.Ambiguous,
            SipStatusCodes.BusyHere,
            SipStatusCodes.RequestTerminated,
            SipStatusCodes.NotAcceptableHere,
            SipStatusCodes.BadEvent,
            SipStatusCodes.RequestPending,
            SipStatusCodes.Undecipherable,
            SipStatusCodes.SecurityAgreementRequired,
            SipStatusCodes.ServerInternalError,
            SipStatusCodes.NotImplemented,
            SipStatusCodes.BadGateway,
            SipStatusCodes.ServiceUnavailable,
            SipStatusCodes.ServerTimeout,
            SipStatusCodes.VersionNotSupported,
            SipStatusCodes.MessageTooLarge,
            SipStatusCodes.PushNotificationServiceNotSupported,
            SipStatusCodes.PreconditionFailure,
            SipStatusCodes.BusyEverywhere,
            SipStatusCodes.Decline,
            SipStatusCodes.DoesNotExistAnywhere,
            SipStatusCodes.GloballyNotAcceptable,
            SipStatusCodes.Unwanted,
            SipStatusCodes.Rejected
        ];

        int[] expected =
        [
            100, 180, 181, 182, 183, 199, 200, 202, 204, 300, 301, 302, 305, 380,
            400, 401, 402, 403, 404, 405, 406, 407, 408, 410, 412, 413, 414, 415,
            416, 417, 420, 421, 422, 423, 424, 425, 428, 429, 430, 433, 436, 437,
            438, 439, 440, 469, 470, 480, 481, 482, 483, 484, 485, 486, 487, 488,
            489, 491, 493, 494, 500, 501, 502, 503, 504, 505, 513, 555, 580, 600,
            603, 604, 606, 607, 608
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ReasonPhrasesMatchConventionalText()
    {
        string[] actual =
        [
            SipReasonPhrases.Trying,
            SipReasonPhrases.Ringing,
            SipReasonPhrases.CallIsBeingForwarded,
            SipReasonPhrases.Queued,
            SipReasonPhrases.SessionProgress,
            SipReasonPhrases.EarlyDialogTerminated,
            SipReasonPhrases.Ok,
            SipReasonPhrases.Accepted,
            SipReasonPhrases.NoNotification,
            SipReasonPhrases.MultipleChoices,
            SipReasonPhrases.MovedPermanently,
            SipReasonPhrases.MovedTemporarily,
            SipReasonPhrases.UseProxy,
            SipReasonPhrases.AlternativeService,
            SipReasonPhrases.BadRequest,
            SipReasonPhrases.Unauthorized,
            SipReasonPhrases.PaymentRequired,
            SipReasonPhrases.Forbidden,
            SipReasonPhrases.NotFound,
            SipReasonPhrases.MethodNotAllowed,
            SipReasonPhrases.NotAcceptable,
            SipReasonPhrases.ProxyAuthenticationRequired,
            SipReasonPhrases.RequestTimeout,
            SipReasonPhrases.Gone,
            SipReasonPhrases.ConditionalRequestFailed,
            SipReasonPhrases.RequestEntityTooLarge,
            SipReasonPhrases.RequestUriTooLong,
            SipReasonPhrases.UnsupportedMediaType,
            SipReasonPhrases.UnsupportedUriScheme,
            SipReasonPhrases.UnknownResourcePriority,
            SipReasonPhrases.BadExtension,
            SipReasonPhrases.ExtensionRequired,
            SipReasonPhrases.SessionIntervalTooSmall,
            SipReasonPhrases.IntervalTooBrief,
            SipReasonPhrases.BadLocationInformation,
            SipReasonPhrases.BadAlertMessage,
            SipReasonPhrases.UseIdentityHeader,
            SipReasonPhrases.ProvideReferrerIdentity,
            SipReasonPhrases.FlowFailed,
            SipReasonPhrases.AnonymityDisallowed,
            SipReasonPhrases.BadIdentityInfo,
            SipReasonPhrases.UnsupportedCertificate,
            SipReasonPhrases.InvalidIdentityHeader,
            SipReasonPhrases.FirstHopLacksOutboundSupport,
            SipReasonPhrases.MaxBreadthExceeded,
            SipReasonPhrases.BadInfoPackage,
            SipReasonPhrases.ConsentNeeded,
            SipReasonPhrases.TemporarilyUnavailable,
            SipReasonPhrases.CallOrTransactionDoesNotExist,
            SipReasonPhrases.LoopDetected,
            SipReasonPhrases.TooManyHops,
            SipReasonPhrases.AddressIncomplete,
            SipReasonPhrases.Ambiguous,
            SipReasonPhrases.BusyHere,
            SipReasonPhrases.RequestTerminated,
            SipReasonPhrases.NotAcceptableHere,
            SipReasonPhrases.BadEvent,
            SipReasonPhrases.RequestPending,
            SipReasonPhrases.Undecipherable,
            SipReasonPhrases.SecurityAgreementRequired,
            SipReasonPhrases.ServerInternalError,
            SipReasonPhrases.NotImplemented,
            SipReasonPhrases.BadGateway,
            SipReasonPhrases.ServiceUnavailable,
            SipReasonPhrases.ServerTimeout,
            SipReasonPhrases.VersionNotSupported,
            SipReasonPhrases.MessageTooLarge,
            SipReasonPhrases.PushNotificationServiceNotSupported,
            SipReasonPhrases.PreconditionFailure,
            SipReasonPhrases.BusyEverywhere,
            SipReasonPhrases.Decline,
            SipReasonPhrases.DoesNotExistAnywhere,
            SipReasonPhrases.GloballyNotAcceptable,
            SipReasonPhrases.Unwanted,
            SipReasonPhrases.Rejected
        ];

        string[] expected =
        [
            "Trying", "Ringing", "Call Is Being Forwarded", "Queued", "Session Progress",
            "Early Dialog Terminated", "OK", "Accepted", "No Notification", "Multiple Choices",
            "Moved Permanently", "Moved Temporarily", "Use Proxy", "Alternative Service", "Bad Request",
            "Unauthorized", "Payment Required", "Forbidden", "Not Found", "Method Not Allowed",
            "Not Acceptable", "Proxy Authentication Required", "Request Timeout", "Gone",
            "Conditional Request Failed", "Request Entity Too Large", "Request-URI Too Long",
            "Unsupported Media Type", "Unsupported URI Scheme", "Unknown Resource-Priority", "Bad Extension",
            "Extension Required", "Session Interval Too Small", "Interval Too Brief", "Bad Location Information",
            "Bad Alert Message", "Use Identity Header", "Provide Referrer Identity", "Flow Failed",
            "Anonymity Disallowed", "Bad Identity-Info", "Unsupported Certificate", "Invalid Identity Header",
            "First Hop Lacks Outbound Support", "Max-Breadth Exceeded", "Bad Info Package", "Consent Needed",
            "Temporarily Unavailable", "Call/Transaction Does Not Exist", "Loop Detected", "Too Many Hops",
            "Address Incomplete", "Ambiguous", "Busy Here", "Request Terminated", "Not Acceptable Here",
            "Bad Event", "Request Pending", "Undecipherable", "Security Agreement Required",
            "Server Internal Error", "Not Implemented", "Bad Gateway", "Service Unavailable", "Server Time-out",
            "Version Not Supported", "Message Too Large", "Push Notification Service Not Supported",
            "Precondition Failure", "Busy Everywhere", "Decline", "Does Not Exist Anywhere", "Not Acceptable",
            "Unwanted", "Rejected"
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void OptionTagsMatchWireTokens()
    {
        string[] actual =
        [
            SipOptionTags.ReliableProvisional,
            SipOptionTags.EarlyDialogTermination,
            SipOptionTags.AnswerMode,
            SipOptionTags.EarlySession,
            SipOptionTags.EventList,
            SipOptionTags.FromChange,
            SipOptionTags.Gruu,
            SipOptionTags.GeolocationHttp,
            SipOptionTags.GeolocationSip,
            SipOptionTags.HistoryInfo,
            SipOptionTags.Ice,
            SipOptionTags.Join,
            SipOptionTags.NoReferSub,
            SipOptionTags.Outbound,
            SipOptionTags.Path,
            SipOptionTags.Precondition,
            SipOptionTags.Preferences,
            SipOptionTags.Privacy,
            SipOptionTags.RecipientListInvite,
            SipOptionTags.RecipientListSubscribe,
            SipOptionTags.Replaces,
            SipOptionTags.ResourcePriority,
            SipOptionTags.SecurityAgreement,
            SipOptionTags.SipRecording,
            SipOptionTags.TargetDialog,
            SipOptionTags.Timer,
            SipOptionTags.TrickleIce
        ];

        string[] expected =
        [
            "100rel", "199", "answermode", "early-session", "eventlist", "from-change", "gruu",
            "geolocation-http", "geolocation-sip", "histinfo", "ice", "join", "norefersub", "outbound",
            "path", "precondition", "pref", "privacy",
            "recipient-list-invite", "recipient-list-subscribe", "replaces", "resource-priority", "sec-agree",
            "siprec", "tdialog", "timer", "trickle-ice"
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void MediaTypesMatchRegisteredValues()
    {
        string[] actual =
        [
            SipMediaTypes.Sip,
            SipMediaTypes.SipFragment,
            SipMediaTypes.Sdp,
            SipMediaTypes.MultipartMixed,
            SipMediaTypes.MultipartAlternative,
            SipMediaTypes.MultipartRelated,
            SipMediaTypes.MultipartSigned,
            SipMediaTypes.MultipartEncrypted,
            SipMediaTypes.Pidf,
            SipMediaTypes.PidfDiff,
            SipMediaTypes.WatcherInfo,
            SipMediaTypes.DialogInfo,
            SipMediaTypes.RegistrationInfo,
            SipMediaTypes.MessageSummary,
            SipMediaTypes.IsComposing,
            SipMediaTypes.ResourceLists,
            SipMediaTypes.ResourceListMetaInformation,
            SipMediaTypes.ConferenceInfo,
            SipMediaTypes.MediaControl,
            SipMediaTypes.RecordingMetadata
        ];

        string[] expected =
        [
            "message/sip", "message/sipfrag", "application/sdp", "multipart/mixed", "multipart/alternative",
            "multipart/related", "multipart/signed", "multipart/encrypted", "application/pidf+xml",
            "application/pidf-diff+xml", "application/watcherinfo+xml", "application/dialog-info+xml",
            "application/reginfo+xml", "application/simple-message-summary", "application/im-iscomposing+xml",
            "application/resource-lists+xml", "application/rlmi+xml", "application/conference-info+xml",
            "application/media_control+xml", "application/rs-metadata+xml"
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TransportConstantsMatchWireValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SipTransportNames.ProtocolVersion, Is.EqualTo("SIP/2.0"));
            Assert.That(SipTransportNames.Udp, Is.EqualTo("UDP"));
            Assert.That(SipTransportNames.Tcp, Is.EqualTo("TCP"));
            Assert.That(SipTransportNames.Tls, Is.EqualTo("TLS"));
            Assert.That(SipTransportNames.Sctp, Is.EqualTo("SCTP"));
            Assert.That(SipTransportNames.TlsSctp, Is.EqualTo("TLS-SCTP"));
            Assert.That(SipTransportNames.WebSocket, Is.EqualTo("WS"));
            Assert.That(SipTransportNames.SecureWebSocket, Is.EqualTo("WSS"));
            Assert.That(SipTransportNames.DatagramTls, Is.EqualTo("DTLS"));
            Assert.That(SipTransportNames.SipPort, Is.EqualTo(5060));
            Assert.That(SipTransportNames.SipsPort, Is.EqualTo(5061));
            Assert.That(SipTransportNames.WebSocketSubprotocol, Is.EqualTo("sip"));
        });
    }

    [Test]
    public void DefaultsExposeExpectedProtocolAndSafetyValues()
    {
        int[] actual =
        [
            SipDefaults.MaxForwards,
            SipDefaults.RegistrationExpiresSeconds,
            SipDefaults.SubscriptionExpiresSeconds,
            SipDefaults.T1Milliseconds,
            SipDefaults.T2Milliseconds,
            SipDefaults.T4Milliseconds,
            SipDefaults.UdpSwitchThresholdBytes,
            SipDefaults.MaxMessageBytes,
            SipDefaults.MaxBodyBytes,
            SipDefaults.MaxHeaderBytes,
            SipDefaults.MaxStartLineBytes,
            SipDefaults.MaxHeaderLineBytes,
            SipDefaults.MaxHeaderCount,
            SipDefaults.MaxUriBytes,
            SipDefaults.MaxParameterCount,
            SipDefaults.MaxListValueCount,
            SipDefaults.MaxMimeDepth,
            SipDefaults.MaxXmlDepth,
            SipDefaults.MaxDnsRecordCount,
            SipDefaults.MaxDnsCnameDepth,
            SipDefaults.MaxRedirectCount,
            SipDefaults.MaxAuthenticationAttempts,
            SipDefaults.MaxQueueCount,
            SipDefaults.MaxTransactionCount,
            SipDefaults.MaxDialogCount,
            SipDefaults.MaxConnectionCount,
            SipDefaults.MaxRegistrationBindingCount,
            SipDefaults.MaxProxyBranchCount
        ];

        int[] expected =
        [
            70, 3600, 3600, 500, 4000, 5000, 1300, 1048576, 1048576, 65536, 8192, 8192,
            256, 4096, 128, 256, 16, 64, 256, 16, 8, 3, 1024, 10000, 10000, 1024, 1000, 64
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TimerNamesAreStableIdentifiers()
    {
        string[] actual =
        [
            SipTimerNames.TimerA,
            SipTimerNames.TimerB,
            SipTimerNames.TimerC,
            SipTimerNames.TimerD,
            SipTimerNames.TimerE,
            SipTimerNames.TimerF,
            SipTimerNames.TimerG,
            SipTimerNames.TimerH,
            SipTimerNames.TimerI,
            SipTimerNames.TimerJ,
            SipTimerNames.TimerK,
            SipTimerNames.TimerL,
            SipTimerNames.TimerM,
            SipTimerNames.ReliableProvisional,
            SipTimerNames.ReliableProvisionalTimeout,
            SipTimerNames.SessionRefresh,
            SipTimerNames.SessionExpires,
            SipTimerNames.RegistrationRefresh,
            SipTimerNames.SubscriptionRefresh
        ];

        string[] expected =
        [
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
            "ReliableProvisional", "ReliableProvisionalTimeout", "SessionRefresh", "SessionExpires",
            "RegistrationRefresh", "SubscriptionRefresh"
        ];

        Assert.That(actual, Is.EqualTo(expected));
    }
}
