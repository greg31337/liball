namespace diameter;

/// <summary>
/// Identifies an RFC 8506 credit-control request type.
/// </summary>
public enum DiameterCreditControlRequestType
{
    /// <summary>Starts a credit-control session.</summary>
    Initial = 1,

    /// <summary>Updates an existing credit-control session.</summary>
    Update = 2,

    /// <summary>Terminates a credit-control session.</summary>
    Termination = 3,

    /// <summary>Executes a stateless credit-control event.</summary>
    Event = 4
}

/// <summary>
/// Identifies an RFC 8506 action requested by a credit-control event.
/// </summary>
public enum DiameterRequestedAction
{
    /// <summary>Debits the subscriber account.</summary>
    DirectDebiting = 0,

    /// <summary>Refunds the subscriber account.</summary>
    RefundAccount = 1,

    /// <summary>Checks the subscriber balance.</summary>
    CheckBalance = 2,

    /// <summary>Requests the price of a service.</summary>
    PriceEnquiry = 3
}

/// <summary>
/// Identifies the lifecycle state of a credit-control session.
/// </summary>
public enum DiameterCreditControlState
{
    /// <summary>No credit-control request is active.</summary>
    Idle = 0,

    /// <summary>An initial request is pending.</summary>
    PendingInitial = 1,

    /// <summary>The credit-control session is open.</summary>
    Open = 2,

    /// <summary>An update request is pending.</summary>
    PendingUpdate = 3,

    /// <summary>A termination request is pending.</summary>
    PendingTermination = 4,

    /// <summary>The credit-control session is closed.</summary>
    Closed = 5,

    /// <summary>The credit-control session failed.</summary>
    Failed = 6
}

/// <summary>
/// Identifies RFC 8506 session credit-control failure handling.
/// </summary>
public enum DiameterCreditControlFailureHandling
{
    /// <summary>Terminates service when credit control fails.</summary>
    Terminate = 0,

    /// <summary>Continues service when credit control fails.</summary>
    Continue = 1,

    /// <summary>Retries an alternate server and otherwise terminates service.</summary>
    RetryAndTerminate = 2
}

/// <summary>
/// Identifies RFC 8506 direct-debit failure handling.
/// </summary>
public enum DiameterDirectDebitFailureHandling
{
    /// <summary>Terminates service or buffers an ambiguous debit request.</summary>
    TerminateOrBuffer = 0,

    /// <summary>Continues service and discards the failed debit request.</summary>
    Continue = 1
}

/// <summary>
/// Identifies a kind of service unit used for credit control.
/// </summary>
public enum DiameterCreditControlUnitType
{
    /// <summary>Measures service time.</summary>
    Time = 0,

    /// <summary>Measures monetary value.</summary>
    Money = 1,

    /// <summary>Measures total octets.</summary>
    TotalOctets = 2,

    /// <summary>Measures input octets.</summary>
    InputOctets = 3,

    /// <summary>Measures output octets.</summary>
    OutputOctets = 4,

    /// <summary>Measures service-specific units.</summary>
    ServiceSpecificUnits = 5
}

/// <summary>
/// Identifies an RFC 8506 final-unit action.
/// </summary>
public enum DiameterFinalUnitAction
{
    /// <summary>Terminates the service session.</summary>
    Terminate = 0,

    /// <summary>Redirects the user.</summary>
    Redirect = 1,

    /// <summary>Restricts the user's access.</summary>
    RestrictAccess = 2
}

/// <summary>
/// Identifies RFC 8506 tariff-change unit usage.
/// </summary>
public enum DiameterTariffChangeUsage
{
    /// <summary>Identifies units used before a tariff change.</summary>
    UnitBeforeTariffChange = 0,

    /// <summary>Identifies units used after a tariff change.</summary>
    UnitAfterTariffChange = 1,

    /// <summary>Identifies units that straddle a tariff change.</summary>
    UnitIndeterminate = 2
}

/// <summary>
/// Identifies RFC 8506 multiple-service credit-control support.
/// </summary>
public enum DiameterMultipleServicesIndicator
{
    /// <summary>Independent multiple-service credit control is not supported.</summary>
    NotSupported = 0,

    /// <summary>Independent multiple-service credit control is supported.</summary>
    Supported = 1
}

/// <summary>
/// Identifies the RFC 8506 result of a balance check.
/// </summary>
public enum DiameterCheckBalanceResult
{
    /// <summary>The account has enough credit.</summary>
    EnoughCredit = 0,

    /// <summary>The account does not have enough credit.</summary>
    NoCredit = 1
}

/// <summary>
/// Identifies the application-level disposition of a credit-control request.
/// </summary>
public enum DiameterCreditControlDisposition
{
    /// <summary>The requested units were granted.</summary>
    Granted = 0,

    /// <summary>Fewer than the requested units were granted.</summary>
    PartiallyGranted = 1,

    /// <summary>The requested units were denied.</summary>
    Denied = 2,

    /// <summary>A final-unit instruction was returned.</summary>
    FinalUnit = 3,

    /// <summary>The request could not be processed.</summary>
    Failed = 4
}

/// <summary>
/// Identifies an RFC 8506 subscription identifier type.
/// </summary>
public enum DiameterSubscriptionIdType
{
    /// <summary>Identifies an E.164 subscriber number.</summary>
    EndUserE164 = 0,

    /// <summary>Identifies an International Mobile Subscriber Identity.</summary>
    EndUserImsi = 1,

    /// <summary>Identifies a Session Initiation Protocol URI.</summary>
    EndUserSipUri = 2,

    /// <summary>Identifies a Network Access Identifier.</summary>
    EndUserNai = 3,

    /// <summary>Identifies a private credit-control server identifier.</summary>
    EndUserPrivate = 4
}

/// <summary>
/// Identifies an RFC 8506 user-equipment information type.
/// </summary>
public enum DiameterUserEquipmentInfoType
{
    /// <summary>Identifies an IMEI with software version.</summary>
    Imeisv = 0,

    /// <summary>Identifies a 48-bit MAC address.</summary>
    Mac = 1,

    /// <summary>Identifies a 64-bit extended unique identifier.</summary>
    Eui64 = 2,

    /// <summary>Identifies a modified EUI-64 value.</summary>
    ModifiedEui64 = 3
}

/// <summary>
/// Identifies an RFC 8506 redirect address type.
/// </summary>
public enum DiameterRedirectAddressType
{
    /// <summary>Identifies a textual IPv4 address.</summary>
    IPv4 = 0,

    /// <summary>Identifies a textual IPv6 address.</summary>
    IPv6 = 1,

    /// <summary>Identifies a URL.</summary>
    Url = 2,

    /// <summary>Identifies a SIP URI.</summary>
    SipUri = 3
}

/// <summary>Classifies privacy-sensitive credit-control AVP content.</summary>
public enum DiameterCreditControlPrivacyClass
{
    /// <summary>The AVP has no credit-control-specific privacy classification.</summary>
    None = 0,

    /// <summary>The AVP identifies a subscriber.</summary>
    SubscriberIdentity = 1,

    /// <summary>The AVP identifies subscriber equipment.</summary>
    EquipmentIdentity = 2,

    /// <summary>The AVP correlates subscriber transactions or sessions.</summary>
    Correlation = 3,

    /// <summary>The AVP describes service use, rating, balance, or cost.</summary>
    ServiceData = 4
}

/// <summary>Selects handling for privacy-sensitive credit-control AVPs.</summary>
public enum DiameterCreditControlPrivacyAction
{
    /// <summary>Preserves the AVP without modification.</summary>
    Preserve = 0,

    /// <summary>Removes the AVP when protocol and routing semantics permit.</summary>
    Remove = 1,

    /// <summary>Replaces sensitive content with type-compatible placeholder data.</summary>
    Redact = 2
}
