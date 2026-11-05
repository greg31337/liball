namespace diameter;

/// <summary>
/// Defines standard Diameter base-protocol and credit-control result codes.
/// </summary>
public static class DiameterResultCodes
{
    /// <summary>Indicates that additional authentication exchanges are required.</summary>
    public const uint MultiRoundAuth = 1001;

    /// <summary>Indicates that the request completed successfully.</summary>
    public const uint Success = 2001;

    /// <summary>Indicates success with additional processing required.</summary>
    public const uint LimitedSuccess = 2002;

    /// <summary>Indicates that the command code is unsupported.</summary>
    public const uint CommandUnsupported = 3001;

    /// <summary>Indicates that the request could not be delivered.</summary>
    public const uint UnableToDeliver = 3002;

    /// <summary>Indicates that the destination realm is not served.</summary>
    public const uint RealmNotServed = 3003;

    /// <summary>Indicates that the node is too busy to process the request.</summary>
    public const uint TooBusy = 3004;

    /// <summary>Indicates that a routing loop was detected.</summary>
    public const uint LoopDetected = 3005;

    /// <summary>Indicates that redirect information is present.</summary>
    public const uint RedirectIndication = 3006;

    /// <summary>Indicates that the requested application is unsupported.</summary>
    public const uint ApplicationUnsupported = 3007;

    /// <summary>Indicates invalid bits in the Diameter header.</summary>
    public const uint InvalidHeaderBits = 3008;

    /// <summary>Indicates invalid bits in an AVP header.</summary>
    public const uint InvalidAvpBits = 3009;

    /// <summary>Indicates that the peer identity is unknown.</summary>
    public const uint UnknownPeer = 3010;

    /// <summary>Indicates that realm-based redirect information is present.</summary>
    public const uint RealmRedirectIndication = 3011;

    /// <summary>Indicates that authentication was rejected.</summary>
    public const uint AuthenticationRejected = 4001;

    /// <summary>Indicates that required storage is unavailable.</summary>
    public const uint OutOfSpace = 4002;

    /// <summary>Indicates loss of a peer connection election.</summary>
    public const uint ElectionLost = 4003;

    /// <summary>Indicates that service was denied to the end user.</summary>
    public const uint EndUserServiceDenied = 4010;

    /// <summary>Indicates that credit control does not apply to the request.</summary>
    public const uint CreditControlNotApplicable = 4011;

    /// <summary>Indicates that the subscriber credit limit was reached.</summary>
    public const uint CreditLimitReached = 4012;

    /// <summary>Indicates that an AVP is unsupported.</summary>
    public const uint AvpUnsupported = 5001;

    /// <summary>Indicates that the Session-Id is unknown.</summary>
    public const uint UnknownSessionId = 5002;

    /// <summary>Indicates that authorization was rejected.</summary>
    public const uint AuthorizationRejected = 5003;

    /// <summary>Indicates that an AVP contains an invalid value.</summary>
    public const uint InvalidAvpValue = 5004;

    /// <summary>Indicates that a required AVP is missing.</summary>
    public const uint MissingAvp = 5005;

    /// <summary>Indicates that required resources were exceeded.</summary>
    public const uint ResourcesExceeded = 5006;

    /// <summary>Indicates that AVPs contradict one another.</summary>
    public const uint ContradictingAvps = 5007;

    /// <summary>Indicates that an AVP is not allowed in the message.</summary>
    public const uint AvpNotAllowed = 5008;

    /// <summary>Indicates that an AVP occurs too many times.</summary>
    public const uint AvpOccursTooManyTimes = 5009;

    /// <summary>Indicates that the peers have no common application.</summary>
    public const uint NoCommonApplication = 5010;

    /// <summary>Indicates that the Diameter version is unsupported.</summary>
    public const uint UnsupportedVersion = 5011;

    /// <summary>Indicates that the request could not be satisfied.</summary>
    public const uint UnableToComply = 5012;

    /// <summary>Indicates an invalid bit in the Diameter header.</summary>
    public const uint InvalidBitInHeader = 5013;

    /// <summary>Indicates an invalid AVP length.</summary>
    public const uint InvalidAvpLength = 5014;

    /// <summary>Indicates an invalid Diameter message length.</summary>
    public const uint InvalidMessageLength = 5015;

    /// <summary>Indicates an invalid AVP flag combination.</summary>
    public const uint InvalidAvpBitCombination = 5016;

    /// <summary>Indicates that the peers have no common security mechanism.</summary>
    public const uint NoCommonSecurity = 5017;

    /// <summary>Indicates that the credit-control user is unknown.</summary>
    public const uint UserUnknown = 5030;

    /// <summary>Indicates that credit-control rating failed.</summary>
    public const uint RatingFailed = 5031;
}
