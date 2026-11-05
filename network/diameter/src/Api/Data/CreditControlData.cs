using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Contains requested, granted, or consumed RFC 8506 service units.</summary>
public class DiameterServiceUnit
{
    /// <summary>The service time in seconds.</summary>
    public uint Time;

    /// <summary>Indicates that service time is present.</summary>
    public bool TimePresent;

    /// <summary>The monetary unit value, or <c>null</c> when absent.</summary>
    public DiameterCostInformation? Money;

    /// <summary>The total number of service octets.</summary>
    public ulong TotalOctets;

    /// <summary>Indicates that total octets are present.</summary>
    public bool TotalOctetsPresent;

    /// <summary>The number of input service octets.</summary>
    public ulong InputOctets;

    /// <summary>Indicates that input octets are present.</summary>
    public bool InputOctetsPresent;

    /// <summary>The number of output service octets.</summary>
    public ulong OutputOctets;

    /// <summary>Indicates that output octets are present.</summary>
    public bool OutputOctetsPresent;

    /// <summary>The number of service-specific units.</summary>
    public ulong ServiceSpecificUnits;

    /// <summary>Indicates that service-specific units are present.</summary>
    public bool ServiceSpecificUnitsPresent;

    /// <summary>The instant at which the granted-unit tariff changes.</summary>
    public DateTimeOffset TariffChangeTime;

    /// <summary>Indicates that a tariff-change time is present.</summary>
    public bool TariffChangeTimePresent;

    /// <summary>Identifies which tariff applies to these used units.</summary>
    public DiameterTariffChangeUsage TariffChangeUsage;

    /// <summary>Indicates that tariff-change usage is present.</summary>
    public bool TariffChangeUsagePresent;

    internal void ValidateCreditControlServiceUnit()
    {
        if (Money != null)
        {
            Money.ValidateCreditControlCostInformation();
        }

        int tariffUsage = (int)TariffChangeUsage;
        if (TariffChangeUsagePresent &&
            (tariffUsage <
                (int)DiameterTariffChangeUsage.UnitBeforeTariffChange ||
            tariffUsage >
                (int)DiameterTariffChangeUsage.UnitIndeterminate))
        {
            throw new ArgumentOutOfRangeException("TariffChangeUsage");
        }

        if (TariffChangeTimePresent &&
            TariffChangeTime == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "A present tariff-change time requires a timestamp.");
        }
    }
}

/// <summary>Contains an RFC 8506 decimal unit value and optional currency metadata.</summary>
public class DiameterCostInformation
{
    /// <summary>The signed significant digits of the decimal value.</summary>
    public long ValueDigits;

    /// <summary>The base-ten exponent applied to the value digits.</summary>
    public int Exponent;

    /// <summary>Indicates that an explicit exponent is present.</summary>
    public bool ExponentPresent;

    /// <summary>The ISO 4217 numeric currency code.</summary>
    public uint CurrencyCode;

    /// <summary>Indicates that a currency code is present.</summary>
    public bool CurrencyCodePresent;

    /// <summary>The display unit associated with cost information.</summary>
    public string CostUnit = string.Empty;

    internal void ValidateCreditControlCostInformation()
    {
        if (CostUnit == null ||
            ValueCodecs.FindInvalidUtf16Index(CostUnit) >= 0)
        {
            throw new ArgumentException(
                "Credit-control cost units must be valid text.");
        }

        if (CurrencyCodePresent &&
            (CurrencyCode == 0U || CurrencyCode > 999U))
        {
            throw new ArgumentOutOfRangeException("CurrencyCode");
        }
    }
}

/// <summary>References one granted-service-unit pool and its conversion value.</summary>
public class DiameterPoolReference
{
    /// <summary>The server-assigned pool identifier.</summary>
    public uint PoolIdentifier;

    /// <summary>The service-unit kind consumed from the pool.</summary>
    public DiameterCreditControlUnitType UnitType;

    /// <summary>The signed conversion value digits.</summary>
    public long ValueDigits;

    /// <summary>The base-ten exponent applied to the conversion value.</summary>
    public int Exponent;

    /// <summary>Indicates that an explicit exponent is present.</summary>
    public bool ExponentPresent;

    internal void ValidateCreditControlPoolReference()
    {
        int unitType = (int)UnitType;
        if (unitType < (int)DiameterCreditControlUnitType.Time ||
            unitType >
                (int)DiameterCreditControlUnitType.ServiceSpecificUnits)
        {
            throw new ArgumentOutOfRangeException("UnitType");
        }
    }
}

/// <summary>Identifies one subscriber using an RFC 8506 subscription identity.</summary>
public class DiameterSubscriptionIdentifier
{
    /// <summary>The namespace of the subscription identifier.</summary>
    public DiameterSubscriptionIdType Type;

    /// <summary>The subscription identifier text.</summary>
    public string Data = string.Empty;

    /// <summary>Indicates use of the RFC 8506 extension AVP form.</summary>
    public bool Extension;

    internal void ValidateCreditControlSubscriptionIdentifier()
    {
        int type = (int)Type;
        if (type < (int)DiameterSubscriptionIdType.EndUserE164 ||
            type > (int)DiameterSubscriptionIdType.EndUserPrivate)
        {
            throw new ArgumentOutOfRangeException("Type");
        }

        if (Data == null || Data.Length == 0 ||
            ValueCodecs.FindInvalidUtf16Index(Data) >= 0)
        {
            throw new ArgumentException(
                "A subscription identifier requires valid text.");
        }
    }
}

/// <summary>Identifies user equipment using legacy or extension RFC 8506 forms.</summary>
public class DiameterUserEquipmentInformation
{
    /// <summary>The namespace of the equipment identifier.</summary>
    public DiameterUserEquipmentInfoType Type;

    /// <summary>The owned equipment identifier octets.</summary>
    public byte[] Value = new byte[0];

    /// <summary>Indicates use of the RFC 8506 extension AVP form.</summary>
    public bool Extension;

    /// <summary>Indicates the extension value is an IMEI rather than an IMEISV.</summary>
    public bool Imei;

    internal void ValidateCreditControlUserEquipmentInformation()
    {
        int type = (int)Type;
        if (type < (int)DiameterUserEquipmentInfoType.Imeisv ||
            type > (int)DiameterUserEquipmentInfoType.ModifiedEui64)
        {
            throw new ArgumentOutOfRangeException("Type");
        }

        if (Value == null || Value.Length == 0)
        {
            throw new ArgumentException(
                "User-equipment information requires owned identifier data.");
        }

        if (Imei && (!Extension ||
            Type != DiameterUserEquipmentInfoType.Imeisv))
        {
            throw new ArgumentException(
                "IMEI is available only through the IMEISV extension form.");
        }
    }
}

/// <summary>Contains an RFC 8506 final-unit service instruction.</summary>
public class DiameterFinalUnitInstruction
{
    /// <summary>The action applied when the final granted units are consumed.</summary>
    public DiameterFinalUnitAction Action;

    /// <summary>The restriction filter rules used by restrict-access.</summary>
    public List<string> RestrictionFilterRules = new List<string>();

    /// <summary>The named filters used by restrict-access.</summary>
    public List<string> FilterIdentifiers = new List<string>();

    /// <summary>The redirect address type.</summary>
    public DiameterRedirectAddressType RedirectAddressType;

    /// <summary>Indicates that a redirect address type is present.</summary>
    public bool RedirectAddressTypePresent;

    /// <summary>The legacy or extension redirect address.</summary>
    public string RedirectAddress = string.Empty;

    /// <summary>Indicates use of the redirect extension AVP form.</summary>
    public bool RedirectExtension;

    /// <summary>Indicates use of QoS final-unit filter rules.</summary>
    public bool Qos;

    internal void ValidateCreditControlFinalUnitInstruction()
    {
        int action = (int)Action;
        int redirectType = (int)RedirectAddressType;
        if (action < (int)DiameterFinalUnitAction.Terminate ||
            action > (int)DiameterFinalUnitAction.RestrictAccess ||
            RedirectAddressTypePresent &&
            (redirectType < (int)DiameterRedirectAddressType.IPv4 ||
            redirectType > (int)DiameterRedirectAddressType.SipUri))
        {
            throw new ArgumentOutOfRangeException(
                "Final-unit action or redirect address type");
        }

        if (RestrictionFilterRules == null ||
            FilterIdentifiers == null || RedirectAddress == null ||
            ValueCodecs.FindInvalidUtf16Index(RedirectAddress) >= 0)
        {
            throw new ArgumentException(
                "Final-unit filters and redirect data must be valid.");
        }

        ValidateCreditControlFinalUnitText(RestrictionFilterRules);
        ValidateCreditControlFinalUnitText(FilterIdentifiers);
        if (Action == DiameterFinalUnitAction.Redirect &&
            (RedirectAddress.Length == 0 ||
            !RedirectExtension && !RedirectAddressTypePresent))
        {
            throw new ArgumentException(
                "A redirect final-unit action requires an address and type.");
        }

        if (Action == DiameterFinalUnitAction.Redirect &&
            RedirectExtension && RedirectAddressTypePresent)
        {
            throw new ArgumentException(
                "An extension redirect infers its address type.");
        }

        if (Action == DiameterFinalUnitAction.Redirect &&
            RedirectExtension != Qos)
        {
            throw new ArgumentException(
                "QoS final-unit redirects require the extension form.");
        }

        if (Action != DiameterFinalUnitAction.Redirect &&
            (RedirectAddressTypePresent || RedirectAddress.Length > 0 ||
            RedirectExtension))
        {
            throw new ArgumentException(
                "Only a redirect final-unit action may carry redirect data.");
        }

        if (Action == DiameterFinalUnitAction.RestrictAccess &&
            RestrictionFilterRules.Count == 0 &&
            FilterIdentifiers.Count == 0)
        {
            throw new ArgumentException(
                "Restrict-access requires at least one filter.");
        }
    }

    internal void ValidateCreditControlFinalUnitText(List<string> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            string value = values[index];
            if (value == null || value.Length == 0 ||
                ValueCodecs.FindInvalidUtf16Index(value) >= 0)
            {
                throw new ArgumentException(
                    "Final-unit filter values must be valid text.");
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains independent credit-control data for one service or rating group.</summary>
public class DiameterMultipleServiceCredit
{
    /// <summary>The local remaining-unit threshold for interrogation, or <c>null</c>.</summary>
    public DiameterServiceUnit? QuotaThreshold;

    /// <summary>The requested units, or <c>null</c> when absent.</summary>
    public DiameterServiceUnit? RequestedUnit;

    /// <summary>The granted units, or <c>null</c> when absent.</summary>
    public DiameterServiceUnit? GrantedUnit;

    /// <summary>The used-unit reports.</summary>
    public List<DiameterServiceUnit> UsedUnits =
        new List<DiameterServiceUnit>();

    /// <summary>The service identifiers sharing this credit decision.</summary>
    public List<uint> ServiceIdentifiers = new List<uint>();

    /// <summary>The rating group.</summary>
    public uint RatingGroup;

    /// <summary>Indicates that a rating group is present.</summary>
    public bool RatingGroupPresent;

    /// <summary>The granted-service-unit pool references.</summary>
    public List<DiameterPoolReference> PoolReferences =
        new List<DiameterPoolReference>();

    /// <summary>The tariff usage associated with the multiple-service group.</summary>
    public DiameterTariffChangeUsage TariffChangeUsage;

    /// <summary>Indicates that tariff usage is present.</summary>
    public bool TariffChangeUsagePresent;

    /// <summary>The validity interval for granted units in milliseconds.</summary>
    public int ValidityTimeMilliseconds;

    /// <summary>Indicates that a validity interval is present.</summary>
    public bool ValidityTimePresent;

    /// <summary>The local absolute deadline for the granted units.</summary>
    public DateTimeOffset ValidityDeadline;

    /// <summary>Indicates that the local validity deadline is active.</summary>
    public bool ValidityDeadlinePresent;

    /// <summary>The service-level Diameter Result-Code.</summary>
    public uint ResultCode;

    /// <summary>Indicates that a service-level Result-Code is present.</summary>
    public bool ResultCodePresent;

    /// <summary>The legacy final-unit instruction, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? FinalUnitInstruction;

    /// <summary>The QoS final-unit instruction, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? QosFinalUnitInstruction;

    /// <summary>The application-level credit disposition.</summary>
    public DiameterCreditControlDisposition Disposition =
        DiameterCreditControlDisposition.Granted;

    internal void ValidateCreditControlMultipleServiceCredit()
    {
        if (UsedUnits == null || ServiceIdentifiers == null ||
            PoolReferences == null)
        {
            throw new ArgumentException(
                "Multiple-service credit collections are required.");
        }

        if (RequestedUnit != null)
        {
            RequestedUnit.ValidateCreditControlServiceUnit();
        }

        if (QuotaThreshold != null)
        {
            QuotaThreshold.ValidateCreditControlServiceUnit();
        }

        if (GrantedUnit != null)
        {
            GrantedUnit.ValidateCreditControlServiceUnit();
        }

        int unitIndex = 0;
        while (unitIndex < UsedUnits.Count)
        {
            DiameterServiceUnit unit = UsedUnits[unitIndex];
            if (unit == null)
            {
                throw new ArgumentException(
                    "Used service units must not contain null values.");
            }

            unit.ValidateCreditControlServiceUnit();
            unitIndex = unitIndex + 1;
        }

        int poolIndex = 0;
        while (poolIndex < PoolReferences.Count)
        {
            DiameterPoolReference reference = PoolReferences[poolIndex];
            if (reference == null)
            {
                throw new ArgumentException(
                    "Pool references must not contain null values.");
            }

            reference.ValidateCreditControlPoolReference();
            poolIndex = poolIndex + 1;
        }

        int tariffUsage = (int)TariffChangeUsage;
        int disposition = (int)Disposition;
        if (TariffChangeUsagePresent &&
            (tariffUsage <
                (int)DiameterTariffChangeUsage.UnitBeforeTariffChange ||
            tariffUsage >
                (int)DiameterTariffChangeUsage.UnitIndeterminate) ||
            disposition <
                (int)DiameterCreditControlDisposition.Granted ||
            disposition >
                (int)DiameterCreditControlDisposition.Failed)
        {
            throw new ArgumentOutOfRangeException(
                "Tariff usage or credit disposition");
        }

        if (ValidityTimePresent &&
            (ValidityTimeMilliseconds <= 0 ||
            ValidityTimeMilliseconds % 1000 != 0) ||
            ValidityDeadlinePresent &&
            ValidityDeadline == default(DateTimeOffset) ||
            ResultCodePresent &&
            (ResultCode < 1000U || ResultCode > 5999U))
        {
            throw new ArgumentOutOfRangeException(
                "Validity time or service result code");
        }

        if (FinalUnitInstruction != null)
        {
            FinalUnitInstruction.ValidateCreditControlFinalUnitInstruction();
        }

        if (QosFinalUnitInstruction != null)
        {
            if (!QosFinalUnitInstruction.Qos)
            {
                throw new ArgumentException(
                    "A QoS final-unit instruction must select QoS form.");
            }

            QosFinalUnitInstruction
                .ValidateCreditControlFinalUnitInstruction();
        }
    }
}

/// <summary>Identifies one credit-control server routing destination.</summary>
public class DiameterCreditControlServer
{
    /// <summary>The server host identity, or <c>null</c> for route-only selection.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The peer identity used to reach the server, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The stable route identifier, or an empty string.</summary>
    public string RouteIdentifier = string.Empty;

    internal void ValidateCreditControlServer()
    {
        if (RouteIdentifier == null ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            DestinationHost == null && PeerIdentity == null &&
            RouteIdentifier.Length == 0 ||
            DestinationHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(DestinationHost).Status !=
                DiameterCodecStatus.Success ||
            PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A valid credit-control server destination is required.");
        }
    }
}

/// <summary>Describes one AVP presented to a credit-control privacy policy.</summary>
public class DiameterCreditControlAvpPrivacy
{
    /// <summary>The AVP code.</summary>
    public uint AvpCode;

    /// <summary>The AVP vendor identifier.</summary>
    public uint VendorId;

    /// <summary>The privacy classification.</summary>
    public DiameterCreditControlPrivacyClass Classification;

    /// <summary>Indicates that the AVP contains privacy-sensitive data.</summary>
    public bool Sensitive;

    /// <summary>Indicates that an agent may require the AVP for routing.</summary>
    public bool RequiredForRouting;

    /// <summary>Indicates that the AVP is required for request correlation.</summary>
    public bool RequiredForCorrelation;

    /// <summary>The agent role processing the message.</summary>
    public DiameterAgentRole AgentRole;

    internal void ValidateCreditControlAvpPrivacy()
    {
        int classification = (int)Classification;
        int role = (int)AgentRole;
        if (classification <
                (int)DiameterCreditControlPrivacyClass.None ||
            classification >
                (int)DiameterCreditControlPrivacyClass.ServiceData ||
            role < (int)DiameterAgentRole.None ||
            role > (int)DiameterAgentRole.Translation ||
            !Sensitive && Classification !=
                DiameterCreditControlPrivacyClass.None ||
            Sensitive && Classification ==
                DiameterCreditControlPrivacyClass.None)
        {
            throw new ArgumentOutOfRangeException(
                "Credit-control AVP privacy classification");
        }
    }
}

/// <summary>Contains one outbound or parsed RFC 8506 Credit-Control-Request.</summary>
public class DiameterCreditControlRequest
{
    /// <summary>The globally unique Session-Id.</summary>
    public string SessionId = string.Empty;

    /// <summary>The destination realm for the credit-control request.</summary>
    public DiameterIdentity DestinationRealm = new DiameterIdentity();

    /// <summary>The destination host, or <c>null</c> when selected by routing.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The preferred peer, or <c>null</c> when selected by routing.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The service-specific rating namespace.</summary>
    public string ServiceContextId = string.Empty;

    /// <summary>The user name associated with the service.</summary>
    public string UserName = string.Empty;

    /// <summary>The INITIAL, UPDATE, TERMINATION, or EVENT request type.</summary>
    public DiameterCreditControlRequestType RequestType =
        DiameterCreditControlRequestType.Initial;

    /// <summary>The monotonically increasing request number.</summary>
    public uint RequestNumber;

    /// <summary>The credit-control sub-session identifier.</summary>
    public ulong CreditControlSubSessionId;

    /// <summary>Indicates that a credit-control sub-session is present.</summary>
    public bool CreditControlSubSessionIdPresent;

    /// <summary>The accounting multi-session correlation text.</summary>
    public string AccountingMultiSessionId = string.Empty;

    /// <summary>The event timestamp.</summary>
    public DateTimeOffset EventTimestamp;

    /// <summary>Indicates that an event timestamp is present.</summary>
    public bool EventTimestampPresent;

    /// <summary>The request origin's restart-state identifier.</summary>
    public uint OriginStateIdentifier;

    /// <summary>Indicates that an origin-state identifier is present.</summary>
    public bool OriginStateIdentifierPresent;

    /// <summary>The service identifiers used for single-service rating.</summary>
    public List<uint> ServiceIdentifiers = new List<uint>();

    /// <summary>The termination cause.</summary>
    public DiameterTerminationCause TerminationCause =
        DiameterTerminationCause.Logout;

    /// <summary>Indicates that a termination cause is present.</summary>
    public bool TerminationCausePresent;

    /// <summary>The requested units for single-service rating, or <c>null</c>.</summary>
    public DiameterServiceUnit? RequestedServiceUnit;

    /// <summary>The local remaining-unit threshold for interrogation, or <c>null</c>.</summary>
    public DiameterServiceUnit? QuotaThreshold;

    /// <summary>Requires a final used-unit report when the session terminates.</summary>
    public bool RequireFinalUsageReport;

    /// <summary>The direct-debit, refund, balance, or price action.</summary>
    public DiameterRequestedAction RequestedAction;

    /// <summary>Indicates that a requested action is present.</summary>
    public bool RequestedActionPresent;

    /// <summary>The used-unit reports for single-service rating.</summary>
    public List<DiameterServiceUnit> UsedServiceUnits =
        new List<DiameterServiceUnit>();

    /// <summary>The negotiated multiple-services support indicator.</summary>
    public DiameterMultipleServicesIndicator MultipleServicesIndicator;

    /// <summary>Indicates that a multiple-services indicator is present.</summary>
    public bool MultipleServicesIndicatorPresent;

    /// <summary>The independently rated service and rating groups.</summary>
    public List<DiameterMultipleServiceCredit> MultipleServiceCredits =
        new List<DiameterMultipleServiceCredit>();

    /// <summary>The opaque service-parameter grouped AVPs.</summary>
    public List<DiameterAvp> ServiceParameterAvps =
        new List<DiameterAvp>();

    /// <summary>The owned CC-Correlation-Id octets.</summary>
    public byte[] CorrelationId = new byte[0];

    /// <summary>The subscriber identifiers used for rating.</summary>
    public List<DiameterSubscriptionIdentifier> SubscriptionIdentifiers =
        new List<DiameterSubscriptionIdentifier>();

    /// <summary>The user-equipment identifier, or <c>null</c> when absent.</summary>
    public DiameterUserEquipmentInformation? UserEquipmentInformation;

    /// <summary>Owned application-specific AVPs appended to the request.</summary>
    public List<DiameterAvp> ServiceAvps = new List<DiameterAvp>();

    /// <summary>The positive transaction timeout in milliseconds.</summary>
    public int TimeoutMilliseconds = 30000;

    /// <summary>Allows an eligible request to fail over to another server.</summary>
    public bool AllowFailover = true;

    /// <summary>Owned alternate servers in deterministic selection order.</summary>
    public List<DiameterCreditControlServer> AlternateServers =
        new List<DiameterCreditControlServer>();

    /// <summary>The local fallback policy for session credit-control failures.</summary>
    public DiameterCreditControlFailureHandling FailureHandling =
        DiameterCreditControlFailureHandling.Terminate;

    /// <summary>The local fallback policy for direct-debit failures.</summary>
    public DiameterDirectDebitFailureHandling DirectDebitFailureHandling =
        DiameterDirectDebitFailureHandling.TerminateOrBuffer;

    /// <summary>The retained route identifier, or an empty string.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>Caller-defined correlation text returned with outcomes.</summary>
    public string CorrelationText = string.Empty;

    /// <summary>Owned application state retained with the operation.</summary>
    public byte[] ApplicationState = new byte[0];

    internal void ValidateCreditControlRequest()
    {
        int requestType = (int)RequestType;
        int requestedAction = (int)RequestedAction;
        int multipleIndicator = (int)MultipleServicesIndicator;
        int terminationCause = (int)TerminationCause;
        int failure = (int)FailureHandling;
        int debitFailure = (int)DirectDebitFailureHandling;
        if (requestType <
                (int)DiameterCreditControlRequestType.Initial ||
            requestType >
                (int)DiameterCreditControlRequestType.Event ||
            RequestedActionPresent &&
            (requestedAction <
                (int)DiameterRequestedAction.DirectDebiting ||
            requestedAction >
                (int)DiameterRequestedAction.PriceEnquiry) ||
            MultipleServicesIndicatorPresent &&
            (multipleIndicator <
                (int)DiameterMultipleServicesIndicator.NotSupported ||
            multipleIndicator >
                (int)DiameterMultipleServicesIndicator.Supported) ||
            TerminationCausePresent &&
            (terminationCause < (int)DiameterTerminationCause.Logout ||
            terminationCause >
                (int)DiameterTerminationCause.SessionTimeout) ||
            failure <
                (int)DiameterCreditControlFailureHandling.Terminate ||
            failure >
                (int)DiameterCreditControlFailureHandling.RetryAndTerminate ||
            debitFailure <
                (int)DiameterDirectDebitFailureHandling.TerminateOrBuffer ||
            debitFailure >
                (int)DiameterDirectDebitFailureHandling.Continue)
        {
            throw new ArgumentOutOfRangeException(
                "Credit-control request enum value");
        }

        if (SessionId == null || SessionId.Length == 0 ||
            ServiceContextId == null || ServiceContextId.Length == 0 ||
            UserName == null || AccountingMultiSessionId == null ||
            ServiceIdentifiers == null || UsedServiceUnits == null ||
            MultipleServiceCredits == null ||
            ServiceParameterAvps == null || CorrelationId == null ||
            SubscriptionIdentifiers == null || ServiceAvps == null ||
            AlternateServers == null ||
            RouteIdentifier == null || CorrelationText == null ||
            ApplicationState == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(ServiceContextId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(UserName) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(
                AccountingMultiSessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(CorrelationText) >= 0)
        {
            throw new ArgumentException(
                "Credit-control request text and owned data are required.");
        }

        if (DestinationRealm == null ||
            ValueCodecs.NormalizeDiameterRealm(DestinationRealm).Status !=
                DiameterCodecStatus.Success ||
            DestinationHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(DestinationHost).Status !=
                DiameterCodecStatus.Success ||
            PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Credit-control routing identities must be valid.");
        }

        if (TimeoutMilliseconds <= 0 ||
            (RequestType == DiameterCreditControlRequestType.Initial ||
            RequestType == DiameterCreditControlRequestType.Event) &&
            RequestNumber != 0U)
        {
            throw new ArgumentOutOfRangeException(
                "Credit-control request number or timeout");
        }

        if (RequestType == DiameterCreditControlRequestType.Event &&
            !RequestedActionPresent ||
            RequestedActionPresent &&
            RequestType != DiameterCreditControlRequestType.Event ||
            TerminationCausePresent &&
            RequestType != DiameterCreditControlRequestType.Termination)
        {
            throw new ArgumentException(
                "Requested action and termination cause must match the request type.");
        }

        if (EventTimestampPresent &&
            EventTimestamp == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "A present event timestamp requires a timestamp.");
        }

        if (RequestedServiceUnit != null)
        {
            RequestedServiceUnit.ValidateCreditControlServiceUnit();
        }

        if (QuotaThreshold != null)
        {
            QuotaThreshold.ValidateCreditControlServiceUnit();
        }

        ValidateCreditControlRequestUnits();
        ValidateCreditControlRequestServices();
        ValidateCreditControlRequestAvps(ServiceParameterAvps);
        ValidateCreditControlRequestAvps(ServiceAvps);
        int alternateIndex = 0;
        while (alternateIndex < AlternateServers.Count)
        {
            DiameterCreditControlServer server =
                AlternateServers[alternateIndex];
            if (server == null)
            {
                throw new ArgumentException(
                    "Alternate credit-control servers must not contain null values.");
            }

            server.ValidateCreditControlServer();
            alternateIndex = alternateIndex + 1;
        }

        if (UserEquipmentInformation != null)
        {
            UserEquipmentInformation
                .ValidateCreditControlUserEquipmentInformation();
        }
    }

    internal void ValidateCreditControlRequestUnits()
    {
        int index = 0;
        while (index < UsedServiceUnits.Count)
        {
            DiameterServiceUnit unit = UsedServiceUnits[index];
            if (unit == null)
            {
                throw new ArgumentException(
                    "Used service units must not contain null values.");
            }

            unit.ValidateCreditControlServiceUnit();
            index = index + 1;
        }

        int creditIndex = 0;
        while (creditIndex < MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                MultipleServiceCredits[creditIndex];
            if (credit == null)
            {
                throw new ArgumentException(
                    "Multiple-service credits must not contain null values.");
            }

            credit.ValidateCreditControlMultipleServiceCredit();
            creditIndex = creditIndex + 1;
        }
    }

    internal void ValidateCreditControlRequestServices()
    {
        int index = 0;
        while (index < SubscriptionIdentifiers.Count)
        {
            DiameterSubscriptionIdentifier identifier =
                SubscriptionIdentifiers[index];
            if (identifier == null)
            {
                throw new ArgumentException(
                    "Subscription identifiers must not contain null values.");
            }

            identifier.ValidateCreditControlSubscriptionIdentifier();
            index = index + 1;
        }
    }

    internal void ValidateCreditControlRequestAvps(List<DiameterAvp> avps)
    {
        int index = 0;
        while (index < avps.Count)
        {
            DiameterAvp avp = avps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Credit-control AVPs must own their payload data.");
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains one outbound or parsed RFC 8506 Credit-Control-Answer.</summary>
public class DiameterCreditControlAnswer
{
    /// <summary>The Session-Id copied from the request.</summary>
    public string SessionId = string.Empty;

    /// <summary>The request type copied from the request.</summary>
    public DiameterCreditControlRequestType RequestType =
        DiameterCreditControlRequestType.Initial;

    /// <summary>The request number copied from the request.</summary>
    public uint RequestNumber;

    /// <summary>The standard Diameter Result-Code.</summary>
    public uint ResultCode = DiameterResultCodes.Success;

    /// <summary>The user name associated with the credit decision.</summary>
    public string UserName = string.Empty;

    /// <summary>Indicates that credit-control session failover is supported.</summary>
    public bool SessionFailoverSupported;

    /// <summary>Indicates that a session-failover decision is present.</summary>
    public bool SessionFailoverPresent;

    /// <summary>The credit-control sub-session identifier.</summary>
    public ulong CreditControlSubSessionId;

    /// <summary>Indicates that a credit-control sub-session is present.</summary>
    public bool CreditControlSubSessionIdPresent;

    /// <summary>The accounting multi-session correlation text.</summary>
    public string AccountingMultiSessionId = string.Empty;

    /// <summary>The event timestamp.</summary>
    public DateTimeOffset EventTimestamp;

    /// <summary>Indicates that an event timestamp is present.</summary>
    public bool EventTimestampPresent;

    /// <summary>The answer origin's restart-state identifier.</summary>
    public uint OriginStateIdentifier;

    /// <summary>Indicates that an origin-state identifier is present.</summary>
    public bool OriginStateIdentifierPresent;

    /// <summary>The granted units for single-service rating, or <c>null</c>.</summary>
    public DiameterServiceUnit? GrantedServiceUnit;

    /// <summary>The independently rated service and rating groups.</summary>
    public List<DiameterMultipleServiceCredit> MultipleServiceCredits =
        new List<DiameterMultipleServiceCredit>();

    /// <summary>The cost information, or <c>null</c> when absent.</summary>
    public DiameterCostInformation? CostInformation;

    /// <summary>The legacy final-unit instruction, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? FinalUnitInstruction;

    /// <summary>The QoS final-unit instruction, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? QosFinalUnitInstruction;

    /// <summary>The result of a requested balance check.</summary>
    public DiameterCheckBalanceResult CheckBalanceResult;

    /// <summary>Indicates that a balance-check result is present.</summary>
    public bool CheckBalanceResultPresent;

    /// <summary>The session credit-control failure policy.</summary>
    public DiameterCreditControlFailureHandling FailureHandling =
        DiameterCreditControlFailureHandling.Terminate;

    /// <summary>Indicates that a session failure policy is present.</summary>
    public bool FailureHandlingPresent;

    /// <summary>The direct-debit failure policy.</summary>
    public DiameterDirectDebitFailureHandling DirectDebitFailureHandling =
        DiameterDirectDebitFailureHandling.TerminateOrBuffer;

    /// <summary>Indicates that a direct-debit failure policy is present.</summary>
    public bool DirectDebitFailureHandlingPresent;

    /// <summary>The validity interval for granted units in milliseconds.</summary>
    public int ValidityTimeMilliseconds;

    /// <summary>Indicates that a validity interval is present.</summary>
    public bool ValidityTimePresent;

    /// <summary>The redirect hosts supplied by the credit-control server.</summary>
    public List<DiameterUri> RedirectHosts = new List<DiameterUri>();

    /// <summary>The scope controlling redirect cache reuse.</summary>
    public DiameterRedirectHostUsage RedirectHostUsage;

    /// <summary>Indicates that redirect-host usage is present.</summary>
    public bool RedirectHostUsagePresent;

    /// <summary>The redirect cache lifetime in milliseconds.</summary>
    public int RedirectMaxCacheTimeMilliseconds;

    /// <summary>Indicates that a redirect cache lifetime is present.</summary>
    public bool RedirectMaxCacheTimePresent;

    /// <summary>The application-level credit disposition.</summary>
    public DiameterCreditControlDisposition Disposition =
        DiameterCreditControlDisposition.Granted;

    /// <summary>The owned answer message, or <c>null</c>.</summary>
    public DiameterMessage? Message;

    /// <summary>Owned application-specific AVPs appended to the answer.</summary>
    public List<DiameterAvp> ServiceAvps = new List<DiameterAvp>();

    /// <summary>The owned AVPs identifying failed request data.</summary>
    public List<DiameterAvp> FailedAvps = new List<DiameterAvp>();

    internal void ValidateCreditControlAnswer()
    {
        int requestType = (int)RequestType;
        int balance = (int)CheckBalanceResult;
        int failure = (int)FailureHandling;
        int debitFailure = (int)DirectDebitFailureHandling;
        int disposition = (int)Disposition;
        int redirectUsage = (int)RedirectHostUsage;
        if (requestType <
                (int)DiameterCreditControlRequestType.Initial ||
            requestType >
                (int)DiameterCreditControlRequestType.Event ||
            ResultCode < 1000U || ResultCode > 5999U ||
            CheckBalanceResultPresent &&
            (balance < (int)DiameterCheckBalanceResult.EnoughCredit ||
            balance > (int)DiameterCheckBalanceResult.NoCredit) ||
            FailureHandlingPresent &&
            (failure <
                (int)DiameterCreditControlFailureHandling.Terminate ||
            failure >
                (int)DiameterCreditControlFailureHandling.RetryAndTerminate) ||
            DirectDebitFailureHandlingPresent &&
            (debitFailure <
                (int)DiameterDirectDebitFailureHandling.TerminateOrBuffer ||
            debitFailure >
                (int)DiameterDirectDebitFailureHandling.Continue) ||
            disposition <
                (int)DiameterCreditControlDisposition.Granted ||
            disposition >
                (int)DiameterCreditControlDisposition.Failed ||
            RedirectHostUsagePresent &&
            (redirectUsage <
                (int)DiameterRedirectHostUsage.DoNotCache ||
            redirectUsage > (int)DiameterRedirectHostUsage.AllUser))
        {
            throw new ArgumentOutOfRangeException(
                "Credit-control answer enum or result value");
        }

        if (SessionId == null || SessionId.Length == 0 ||
            UserName == null || AccountingMultiSessionId == null ||
            MultipleServiceCredits == null || RedirectHosts == null ||
            ServiceAvps == null || FailedAvps == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(UserName) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(
                AccountingMultiSessionId) >= 0 ||
            Message != null && Message.Avps == null)
        {
            throw new ArgumentException(
                "Credit-control answer correlation and owned data are required.");
        }

        if (ValidityTimePresent &&
            (ValidityTimeMilliseconds <= 0 ||
            ValidityTimeMilliseconds % 1000 != 0))
        {
            throw new ArgumentOutOfRangeException(
                "ValidityTimeMilliseconds");
        }

        if (RedirectMaxCacheTimePresent &&
            (RedirectMaxCacheTimeMilliseconds < 0 ||
            RedirectMaxCacheTimeMilliseconds % 1000 != 0))
        {
            throw new ArgumentOutOfRangeException(
                "RedirectMaxCacheTimeMilliseconds");
        }

        if (EventTimestampPresent &&
            EventTimestamp == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "A present event timestamp requires a timestamp.");
        }

        if (GrantedServiceUnit != null)
        {
            GrantedServiceUnit.ValidateCreditControlServiceUnit();
        }

        ValidateCreditControlAnswerGroups();
        ValidateCreditControlAnswerRedirects();
        ValidateCreditControlAnswerAvps();
    }

    internal void ValidateCreditControlAnswerGroups()
    {
        int index = 0;
        while (index < MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                MultipleServiceCredits[index];
            if (credit == null)
            {
                throw new ArgumentException(
                    "Multiple-service credits must not contain null values.");
            }

            credit.ValidateCreditControlMultipleServiceCredit();
            index = index + 1;
        }

        if (CostInformation != null)
        {
            CostInformation.ValidateCreditControlCostInformation();
            if (!CostInformation.CurrencyCodePresent)
            {
                throw new ArgumentException(
                    "Cost information requires a currency code.");
            }
        }

        if (FinalUnitInstruction != null)
        {
            FinalUnitInstruction.ValidateCreditControlFinalUnitInstruction();
        }

        if (QosFinalUnitInstruction != null)
        {
            if (!QosFinalUnitInstruction.Qos)
            {
                throw new ArgumentException(
                    "A QoS final-unit instruction must select QoS form.");
            }

            QosFinalUnitInstruction
                .ValidateCreditControlFinalUnitInstruction();
        }
    }

    internal void ValidateCreditControlAnswerRedirects()
    {
        int index = 0;
        while (index < RedirectHosts.Count)
        {
            DiameterUri redirect = RedirectHosts[index];
            DiameterValueResult normalized =
                ValueCodecs.NormalizeDiameterUri(redirect);
            if (redirect == null ||
                normalized.Status != DiameterCodecStatus.Success ||
                normalized.Uri == null ||
                normalized.Uri.Protocol != "diameter")
            {
                throw new ArgumentException(
                    "Credit-control redirect hosts must be Diameter URIs.");
            }

            index = index + 1;
        }
    }

    internal void ValidateCreditControlAnswerAvps()
    {
        int index = 0;
        while (index < ServiceAvps.Count)
        {
            DiameterAvp avp = ServiceAvps[index];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Credit-control answer AVPs must own their payload data.");
            }

            index = index + 1;
        }

        int failedIndex = 0;
        while (failedIndex < FailedAvps.Count)
        {
            DiameterAvp avp = FailedAvps[failedIndex];
            if (avp == null || avp.Data == null)
            {
                throw new ArgumentException(
                    "Failed AVPs must own their payload data.");
            }

            failedIndex = failedIndex + 1;
        }
    }
}

/// <summary>Contains persistable RFC 8506 client or server session state.</summary>
public class DiameterCreditControlSession
{
    /// <summary>The globally unique Session-Id.</summary>
    public string SessionId = string.Empty;

    /// <summary>The optimistic persistence version.</summary>
    public long Version;

    /// <summary>The local role in the credit-control session.</summary>
    public DiameterSessionRole Role = DiameterSessionRole.Client;

    /// <summary>The current credit-control lifecycle state.</summary>
    public DiameterCreditControlState State =
        DiameterCreditControlState.Idle;

    /// <summary>The service-specific rating namespace.</summary>
    public string ServiceContextId = string.Empty;

    /// <summary>The destination realm used by client requests.</summary>
    public DiameterIdentity? DestinationRealm;

    /// <summary>The destination host retained for affinity, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The currently bound peer, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The user name associated with the session.</summary>
    public string UserName = string.Empty;

    /// <summary>The next request number assigned by a client.</summary>
    public uint NextRequestNumber;

    /// <summary>The pending request type.</summary>
    public DiameterCreditControlRequestType PendingRequestType =
        DiameterCreditControlRequestType.Initial;

    /// <summary>The pending request number.</summary>
    public uint PendingRequestNumber;

    /// <summary>Indicates that a request is awaiting a terminal outcome.</summary>
    public bool PendingRequestPresent;

    /// <summary>The local outbound request identifier awaiting an outcome.</summary>
    public long PendingRequestIdentifier;

    /// <summary>The local identifier of the most recently completed request.</summary>
    public long LastCompletedRequestIdentifier;

    /// <summary>The credit-control sub-session identifier.</summary>
    public ulong CreditControlSubSessionId;

    /// <summary>Indicates that a credit-control sub-session is present.</summary>
    public bool CreditControlSubSessionIdPresent;

    /// <summary>The accounting multi-session correlation text.</summary>
    public string AccountingMultiSessionId = string.Empty;

    /// <summary>The owned CC-Correlation-Id octets.</summary>
    public byte[] CorrelationId = new byte[0];

    /// <summary>The requested units for the current quota, or <c>null</c>.</summary>
    public DiameterServiceUnit? RequestedServiceUnit;

    /// <summary>The granted units for the current quota, or <c>null</c>.</summary>
    public DiameterServiceUnit? GrantedServiceUnit;

    /// <summary>The cumulative used units for the current quota, or <c>null</c>.</summary>
    public DiameterServiceUnit? UsedServiceUnit;

    /// <summary>The retained remaining-unit threshold, or <c>null</c>.</summary>
    public DiameterServiceUnit? QuotaThreshold;

    /// <summary>The independently rated service and rating groups.</summary>
    public List<DiameterMultipleServiceCredit> MultipleServiceCredits =
        new List<DiameterMultipleServiceCredit>();

    /// <summary>The negotiated multiple-services support indicator.</summary>
    public DiameterMultipleServicesIndicator MultipleServicesIndicator;

    /// <summary>Indicates that multiple-services support was negotiated.</summary>
    public bool MultipleServicesIndicatorPresent;

    /// <summary>The active granted-service-unit pool references.</summary>
    public List<DiameterPoolReference> PoolReferences =
        new List<DiameterPoolReference>();

    /// <summary>The active final-unit instruction, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? FinalUnitInstruction;

    /// <summary>The active QoS final-unit instruction, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? QosFinalUnitInstruction;

    /// <summary>The final-unit action applied after quota exhaustion, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? AppliedFinalUnitInstruction;

    /// <summary>Indicates that service should end gracefully after final usage reporting.</summary>
    public bool GracefulTerminationRequired;

    /// <summary>The most recently supplied cost information, or <c>null</c>.</summary>
    public DiameterCostInformation? CostInformation;

    /// <summary>The subscriber identifiers retained for rating.</summary>
    public List<DiameterSubscriptionIdentifier> SubscriptionIdentifiers =
        new List<DiameterSubscriptionIdentifier>();

    /// <summary>The retained user-equipment identifier, or <c>null</c>.</summary>
    public DiameterUserEquipmentInformation? UserEquipmentInformation;

    /// <summary>The session credit-control failure policy.</summary>
    public DiameterCreditControlFailureHandling FailureHandling =
        DiameterCreditControlFailureHandling.Terminate;

    /// <summary>The direct-debit failure policy.</summary>
    public DiameterDirectDebitFailureHandling DirectDebitFailureHandling =
        DiameterDirectDebitFailureHandling.TerminateOrBuffer;

    /// <summary>Indicates that server failover was negotiated.</summary>
    public bool SessionFailoverSupported;

    /// <summary>The primary server retained for failback, or <c>null</c>.</summary>
    public DiameterCreditControlServer? PrimaryServer;

    /// <summary>Owned alternate servers in deterministic selection order.</summary>
    public List<DiameterCreditControlServer> AlternateServers =
        new List<DiameterCreditControlServer>();

    /// <summary>Stable keys of servers attempted for the pending request.</summary>
    public List<string> AttemptedServers = new List<string>();

    /// <summary>Indicates that route affinity currently targets an alternate server.</summary>
    public bool FailoverActive;

    /// <summary>The most recent application disposition.</summary>
    public DiameterCreditControlDisposition Disposition =
        DiameterCreditControlDisposition.Granted;

    /// <summary>The route retained for session affinity.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>The last observed peer Origin-State-Id.</summary>
    public uint PeerOriginStateIdentifier;

    /// <summary>Indicates that a peer Origin-State-Id is present.</summary>
    public bool PeerOriginStateIdentifierPresent;

    /// <summary>The session creation time.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The last session update time.</summary>
    public DateTimeOffset UpdatedAt;

    /// <summary>The pending transaction deadline.</summary>
    public DateTimeOffset TransactionDeadline;

    /// <summary>Indicates that a transaction deadline is active.</summary>
    public bool TransactionDeadlinePresent;

    /// <summary>The current granted-unit validity deadline.</summary>
    public DateTimeOffset ValidityDeadline;

    /// <summary>Indicates that a validity deadline is active.</summary>
    public bool ValidityDeadlinePresent;

    /// <summary>The tariff-change instant.</summary>
    public DateTimeOffset TariffChangeTime;

    /// <summary>Indicates that a tariff-change instant is active.</summary>
    public bool TariffChangeTimePresent;

    /// <summary>The scheduled intermediate interrogation time.</summary>
    public DateTimeOffset NextInterrogationTime;

    /// <summary>Indicates that an intermediate interrogation is scheduled.</summary>
    public bool NextInterrogationTimePresent;

    /// <summary>Indicates that the configured remaining-unit threshold was reached.</summary>
    public bool QuotaThresholdReached;

    /// <summary>Indicates that at least one active granted quota was exhausted.</summary>
    public bool QuotaExhausted;

    /// <summary>Indicates that termination must carry final used-unit data.</summary>
    public bool FinalUsageReportRequired;

    /// <summary>Indicates that a server-initiated credit re-authorization is pending.</summary>
    public bool ReauthorizationRequested;

    /// <summary>The base-session re-authorization operation requested by the server.</summary>
    public DiameterReAuthRequestType ReAuthRequestType =
        DiameterReAuthRequestType.AuthorizeOnly;

    /// <summary>The most recent owned request, or <c>null</c>.</summary>
    public DiameterCreditControlRequest? LastRequest;

    /// <summary>The most recent owned answer, or <c>null</c>.</summary>
    public DiameterCreditControlAnswer? LastAnswer;

    /// <summary>The most recent operation result, or <c>null</c>.</summary>
    public DiameterCreditControlResult? LastOperationResult;

    /// <summary>The number of send attempts for the pending request.</summary>
    public int AttemptCount;

    /// <summary>Owned application-defined persisted state.</summary>
    public byte[] ApplicationState = new byte[0];

    /// <summary>The stable machine-readable session outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable session outcome description.</summary>
    public string Message = string.Empty;

    internal void ValidateCreditControlSession()
    {
        int role = (int)Role;
        int state = (int)State;
        int pendingType = (int)PendingRequestType;
        int failure = (int)FailureHandling;
        int debitFailure = (int)DirectDebitFailureHandling;
        int disposition = (int)Disposition;
        int multipleIndicator = (int)MultipleServicesIndicator;
        int reAuthRequestType = (int)ReAuthRequestType;
        if (role < (int)DiameterSessionRole.Client ||
            role > (int)DiameterSessionRole.Server ||
            state < (int)DiameterCreditControlState.Idle ||
            state > (int)DiameterCreditControlState.Failed ||
            PendingRequestPresent &&
            (pendingType <
                (int)DiameterCreditControlRequestType.Initial ||
            pendingType >
                (int)DiameterCreditControlRequestType.Event) ||
            failure <
                (int)DiameterCreditControlFailureHandling.Terminate ||
            failure >
                (int)DiameterCreditControlFailureHandling.RetryAndTerminate ||
            debitFailure <
                (int)DiameterDirectDebitFailureHandling.TerminateOrBuffer ||
            debitFailure >
                (int)DiameterDirectDebitFailureHandling.Continue ||
            disposition <
                (int)DiameterCreditControlDisposition.Granted ||
            disposition >
                (int)DiameterCreditControlDisposition.Failed ||
            MultipleServicesIndicatorPresent &&
            (multipleIndicator <
                (int)DiameterMultipleServicesIndicator.NotSupported ||
            multipleIndicator >
                (int)DiameterMultipleServicesIndicator.Supported) ||
            reAuthRequestType <
                (int)DiameterReAuthRequestType.AuthorizeOnly ||
            reAuthRequestType >
                (int)DiameterReAuthRequestType.AuthorizeAuthenticate ||
            Version < 0 || AttemptCount < 0 ||
            PendingRequestIdentifier < 0 ||
            LastCompletedRequestIdentifier < 0)
        {
            throw new ArgumentOutOfRangeException(
                "Credit-control session state or counter");
        }

        if (SessionId == null || SessionId.Length == 0 ||
            ServiceContextId == null || ServiceContextId.Length == 0 ||
            UserName == null || AccountingMultiSessionId == null ||
            CorrelationId == null || MultipleServiceCredits == null ||
            PoolReferences == null || SubscriptionIdentifiers == null ||
            AlternateServers == null || AttemptedServers == null ||
            RouteIdentifier == null || ApplicationState == null ||
            Code == null || Message == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(ServiceContextId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(UserName) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(
                AccountingMultiSessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Message) >= 0)
        {
            throw new ArgumentException(
                "Credit-control session text and owned data are required.");
        }

        if (Role == DiameterSessionRole.Client &&
            (DestinationRealm == null ||
            ValueCodecs.NormalizeDiameterRealm(DestinationRealm).Status !=
                DiameterCodecStatus.Success) ||
            DestinationHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(DestinationHost).Status !=
                DiameterCodecStatus.Success ||
            PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Credit-control session routing identities must be valid.");
        }

        if (CreatedAt == default(DateTimeOffset) || UpdatedAt < CreatedAt ||
            TransactionDeadlinePresent &&
            TransactionDeadline < UpdatedAt ||
            ValidityDeadlinePresent && ValidityDeadline < UpdatedAt ||
            TariffChangeTimePresent &&
            TariffChangeTime < CreatedAt ||
            NextInterrogationTimePresent &&
            NextInterrogationTime < UpdatedAt)
        {
            throw new ArgumentException(
                "Credit-control session timestamps are inconsistent.");
        }

        if (PendingRequestPresent && PendingRequestIdentifier == 0 ||
            !PendingRequestPresent &&
            (TransactionDeadlinePresent || PendingRequestIdentifier != 0))
        {
            throw new ArgumentException(
                "Pending request correlation and deadline state are inconsistent.");
        }

        ValidateCreditControlSessionUnits();
        ValidateCreditControlSessionCollections();
        if (LastRequest != null)
        {
            LastRequest.ValidateCreditControlRequest();
        }

        if (LastAnswer != null)
        {
            LastAnswer.ValidateCreditControlAnswer();
        }
    }

    internal void ValidateCreditControlSessionUnits()
    {
        if (RequestedServiceUnit != null)
        {
            RequestedServiceUnit.ValidateCreditControlServiceUnit();
        }

        if (GrantedServiceUnit != null)
        {
            GrantedServiceUnit.ValidateCreditControlServiceUnit();
        }

        if (UsedServiceUnit != null)
        {
            UsedServiceUnit.ValidateCreditControlServiceUnit();
        }

        if (QuotaThreshold != null)
        {
            QuotaThreshold.ValidateCreditControlServiceUnit();
        }

        if (FinalUnitInstruction != null)
        {
            FinalUnitInstruction.ValidateCreditControlFinalUnitInstruction();
        }

        if (QosFinalUnitInstruction != null)
        {
            QosFinalUnitInstruction
                .ValidateCreditControlFinalUnitInstruction();
        }

        if (AppliedFinalUnitInstruction != null)
        {
            AppliedFinalUnitInstruction
                .ValidateCreditControlFinalUnitInstruction();
        }

        if (CostInformation != null)
        {
            CostInformation.ValidateCreditControlCostInformation();
        }

        if (UserEquipmentInformation != null)
        {
            UserEquipmentInformation
                .ValidateCreditControlUserEquipmentInformation();
        }
    }

    internal void ValidateCreditControlSessionCollections()
    {
        int creditIndex = 0;
        while (creditIndex < MultipleServiceCredits.Count)
        {
            DiameterMultipleServiceCredit credit =
                MultipleServiceCredits[creditIndex];
            if (credit == null)
            {
                throw new ArgumentException(
                    "Multiple-service credits must not contain null values.");
            }

            credit.ValidateCreditControlMultipleServiceCredit();
            creditIndex = creditIndex + 1;
        }

        int poolIndex = 0;
        while (poolIndex < PoolReferences.Count)
        {
            DiameterPoolReference reference = PoolReferences[poolIndex];
            if (reference == null)
            {
                throw new ArgumentException(
                    "Pool references must not contain null values.");
            }

            reference.ValidateCreditControlPoolReference();
            poolIndex = poolIndex + 1;
        }

        int subscriptionIndex = 0;
        while (subscriptionIndex < SubscriptionIdentifiers.Count)
        {
            DiameterSubscriptionIdentifier identifier =
                SubscriptionIdentifiers[subscriptionIndex];
            if (identifier == null)
            {
                throw new ArgumentException(
                    "Subscription identifiers must not contain null values.");
            }

            identifier.ValidateCreditControlSubscriptionIdentifier();
            subscriptionIndex = subscriptionIndex + 1;
        }

        if (PrimaryServer != null)
        {
            PrimaryServer.ValidateCreditControlServer();
        }

        int alternateIndex = 0;
        while (alternateIndex < AlternateServers.Count)
        {
            DiameterCreditControlServer server =
                AlternateServers[alternateIndex];
            if (server == null)
            {
                throw new ArgumentException(
                    "Alternate credit-control servers must not contain null values.");
            }

            server.ValidateCreditControlServer();
            alternateIndex = alternateIndex + 1;
        }

        int attemptedIndex = 0;
        while (attemptedIndex < AttemptedServers.Count)
        {
            string key = AttemptedServers[attemptedIndex];
            if (key == null || key.Length == 0 ||
                ValueCodecs.FindInvalidUtf16Index(key) >= 0)
            {
                throw new ArgumentException(
                    "Attempted credit-control server keys must be valid.");
            }

            attemptedIndex = attemptedIndex + 1;
        }
    }
}

/// <summary>Contains one credit-control client or server operation outcome.</summary>
public class DiameterCreditControlResult
{
    /// <summary>The request represented by the outcome.</summary>
    public DiameterCreditControlRequest Request =
        new DiameterCreditControlRequest();

    /// <summary>The correlated answer, or <c>null</c> when unavailable.</summary>
    public DiameterCreditControlAnswer? Answer;

    /// <summary>The current session snapshot, or <c>null</c> for an event.</summary>
    public DiameterCreditControlSession? Session;

    /// <summary>The current credit-control lifecycle state.</summary>
    public DiameterCreditControlState State =
        DiameterCreditControlState.Idle;

    /// <summary>The application-level credit disposition.</summary>
    public DiameterCreditControlDisposition Disposition =
        DiameterCreditControlDisposition.Granted;

    /// <summary>Indicates that the operation was admitted.</summary>
    public bool Accepted;

    /// <summary>Indicates that the request reached a peer transport.</summary>
    public bool Delivered;

    /// <summary>Indicates that a correlated answer was received.</summary>
    public bool AnswerReceived;

    /// <summary>Indicates successful protocol and application processing.</summary>
    public bool Successful;

    /// <summary>Indicates that the request duplicated an observed transaction.</summary>
    public bool Duplicate;

    /// <summary>Indicates that the request was retransmitted.</summary>
    public bool Retransmission;

    /// <summary>Indicates that an alternate server was attempted.</summary>
    public bool FailoverAttempted;

    /// <summary>Indicates that another delivery attempt should be made.</summary>
    public bool RetryRequired;

    /// <summary>Indicates that the request should be retained for later delivery.</summary>
    public bool BufferRequest;

    /// <summary>Indicates that service may continue.</summary>
    public bool ContinueService;

    /// <summary>Indicates that service must terminate.</summary>
    public bool TerminateService;

    /// <summary>Indicates that granted quota is exhausted.</summary>
    public bool QuotaExhausted;

    /// <summary>Indicates that a configured remaining-unit threshold was reached.</summary>
    public bool QuotaThresholdReached;

    /// <summary>Indicates that a final-unit instruction is active.</summary>
    public bool FinalUnit;

    /// <summary>The final-unit action applied after quota exhaustion, or <c>null</c>.</summary>
    public DiameterFinalUnitInstruction? AppliedFinalUnitInstruction;

    /// <summary>Indicates that service should terminate after graceful final reporting.</summary>
    public bool GracefulTerminationRequired;

    /// <summary>The standard Diameter Result-Code, or zero when unavailable.</summary>
    public uint ResultCode;

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;

    internal void ValidateCreditControlResult()
    {
        int state = (int)State;
        int disposition = (int)Disposition;
        if (state < (int)DiameterCreditControlState.Idle ||
            state > (int)DiameterCreditControlState.Failed ||
            disposition <
                (int)DiameterCreditControlDisposition.Granted ||
            disposition >
                (int)DiameterCreditControlDisposition.Failed ||
            ResultCode != 0U &&
            (ResultCode < 1000U || ResultCode > 5999U))
        {
            throw new ArgumentOutOfRangeException(
                "Credit-control result state or code");
        }

        if (Request == null || Code == null || Message == null ||
            ValueCodecs.FindInvalidUtf16Index(Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Message) >= 0)
        {
            throw new ArgumentException(
                "A credit-control result requires request and outcome data.");
        }

        Request.ValidateCreditControlRequest();
        if (Answer != null)
        {
            Answer.ValidateCreditControlAnswer();
        }

        if (AnswerReceived && Answer == null)
        {
            throw new ArgumentException(
                "An answer-received outcome requires an answer.");
        }

        if (ContinueService && TerminateService ||
            RetryRequired && TerminateService)
        {
            throw new ArgumentException(
                "Service continuation or retry cannot accompany termination.");
        }
    }
}
