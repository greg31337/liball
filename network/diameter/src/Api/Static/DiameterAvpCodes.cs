namespace diameter;

/// <summary>
/// Defines AVP codes used by the built-in Diameter dictionaries.
/// </summary>
public static class DiameterAvpCodes
{
    /// <summary>The User-Name AVP code.</summary>
    public const uint UserName = 1;

    /// <summary>The Filter-Id AVP code reused by credit control.</summary>
    public const uint FilterId = 11;

    /// <summary>The Class AVP code.</summary>
    public const uint Class = 25;

    /// <summary>The Session-Timeout AVP code.</summary>
    public const uint SessionTimeout = 27;

    /// <summary>The Proxy-State AVP code.</summary>
    public const uint ProxyState = 33;

    /// <summary>The Acct-Session-Id AVP code.</summary>
    public const uint AcctSessionId = 44;

    /// <summary>The Acct-Multi-Session-Id AVP code.</summary>
    public const uint AcctMultiSessionId = 50;

    /// <summary>The Event-Timestamp AVP code.</summary>
    public const uint EventTimestamp = 55;

    /// <summary>The Acct-Interim-Interval AVP code.</summary>
    public const uint AcctInterimInterval = 85;

    /// <summary>The Host-IP-Address AVP code.</summary>
    public const uint HostIpAddress = 257;

    /// <summary>The Auth-Application-Id AVP code.</summary>
    public const uint AuthApplicationId = 258;

    /// <summary>The Acct-Application-Id AVP code.</summary>
    public const uint AcctApplicationId = 259;

    /// <summary>The Vendor-Specific-Application-Id AVP code.</summary>
    public const uint VendorSpecificApplicationId = 260;

    /// <summary>The Redirect-Host-Usage AVP code.</summary>
    public const uint RedirectHostUsage = 261;

    /// <summary>The Redirect-Max-Cache-Time AVP code.</summary>
    public const uint RedirectMaxCacheTime = 262;

    /// <summary>The Session-Id AVP code.</summary>
    public const uint SessionId = 263;

    /// <summary>The Origin-Host AVP code.</summary>
    public const uint OriginHost = 264;

    /// <summary>The Supported-Vendor-Id AVP code.</summary>
    public const uint SupportedVendorId = 265;

    /// <summary>The Vendor-Id AVP code.</summary>
    public const uint VendorId = 266;

    /// <summary>The Firmware-Revision AVP code.</summary>
    public const uint FirmwareRevision = 267;

    /// <summary>The Result-Code AVP code.</summary>
    public const uint ResultCode = 268;

    /// <summary>The Product-Name AVP code.</summary>
    public const uint ProductName = 269;

    /// <summary>The Session-Binding AVP code.</summary>
    public const uint SessionBinding = 270;

    /// <summary>The Session-Server-Failover AVP code.</summary>
    public const uint SessionServerFailover = 271;

    /// <summary>The Multi-Round-Time-Out AVP code.</summary>
    public const uint MultiRoundTimeOut = 272;

    /// <summary>The Disconnect-Cause AVP code.</summary>
    public const uint DisconnectCause = 273;

    /// <summary>The Auth-Request-Type AVP code.</summary>
    public const uint AuthRequestType = 274;

    /// <summary>The Auth-Grace-Period AVP code.</summary>
    public const uint AuthGracePeriod = 276;

    /// <summary>The Auth-Session-State AVP code.</summary>
    public const uint AuthSessionState = 277;

    /// <summary>The Origin-State-Id AVP code.</summary>
    public const uint OriginStateId = 278;

    /// <summary>The Failed-AVP AVP code.</summary>
    public const uint FailedAvp = 279;

    /// <summary>The Proxy-Host AVP code.</summary>
    public const uint ProxyHost = 280;

    /// <summary>The Error-Message AVP code.</summary>
    public const uint ErrorMessage = 281;

    /// <summary>The Route-Record AVP code.</summary>
    public const uint RouteRecord = 282;

    /// <summary>The Destination-Realm AVP code.</summary>
    public const uint DestinationRealm = 283;

    /// <summary>The Proxy-Info AVP code.</summary>
    public const uint ProxyInfo = 284;

    /// <summary>The Re-Auth-Request-Type AVP code.</summary>
    public const uint ReAuthRequestType = 285;

    /// <summary>The Accounting-Sub-Session-Id AVP code.</summary>
    public const uint AccountingSubSessionId = 287;

    /// <summary>The Authorization-Lifetime AVP code.</summary>
    public const uint AuthorizationLifetime = 291;

    /// <summary>The Redirect-Host AVP code.</summary>
    public const uint RedirectHost = 292;

    /// <summary>The Destination-Host AVP code.</summary>
    public const uint DestinationHost = 293;

    /// <summary>The Error-Reporting-Host AVP code.</summary>
    public const uint ErrorReportingHost = 294;

    /// <summary>The Termination-Cause AVP code.</summary>
    public const uint TerminationCause = 295;

    /// <summary>The Origin-Realm AVP code.</summary>
    public const uint OriginRealm = 296;

    /// <summary>The Experimental-Result AVP code.</summary>
    public const uint ExperimentalResult = 297;

    /// <summary>The Experimental-Result-Code AVP code.</summary>
    public const uint ExperimentalResultCode = 298;

    /// <summary>The Inband-Security-Id AVP code.</summary>
    public const uint InbandSecurityId = 299;

    /// <summary>The E2E-Sequence AVP code.</summary>
    public const uint E2ESequence = 300;

    /// <summary>The Diameter Routing Message Priority AVP code.</summary>
    public const uint Drmp = 301;

    /// <summary>The QoS-Filter-Rule AVP code.</summary>
    public const uint QosFilterRule = 407;

    /// <summary>The CC-Correlation-Id AVP code.</summary>
    public const uint CcCorrelationId = 411;

    /// <summary>The CC-Input-Octets AVP code.</summary>
    public const uint CcInputOctets = 412;

    /// <summary>The CC-Money AVP code.</summary>
    public const uint CcMoney = 413;

    /// <summary>The CC-Output-Octets AVP code.</summary>
    public const uint CcOutputOctets = 414;

    /// <summary>The CC-Request-Number AVP code.</summary>
    public const uint CcRequestNumber = 415;

    /// <summary>The CC-Request-Type AVP code.</summary>
    public const uint CcRequestType = 416;

    /// <summary>The CC-Service-Specific-Units AVP code.</summary>
    public const uint CcServiceSpecificUnits = 417;

    /// <summary>The CC-Session-Failover AVP code.</summary>
    public const uint CcSessionFailover = 418;

    /// <summary>The CC-Sub-Session-Id AVP code.</summary>
    public const uint CcSubSessionId = 419;

    /// <summary>The CC-Time AVP code.</summary>
    public const uint CcTime = 420;

    /// <summary>The CC-Total-Octets AVP code.</summary>
    public const uint CcTotalOctets = 421;

    /// <summary>The Check-Balance-Result AVP code.</summary>
    public const uint CheckBalanceResult = 422;

    /// <summary>The Cost-Information AVP code.</summary>
    public const uint CostInformation = 423;

    /// <summary>The Cost-Unit AVP code.</summary>
    public const uint CostUnit = 424;

    /// <summary>The Currency-Code AVP code.</summary>
    public const uint CurrencyCode = 425;

    /// <summary>The Credit-Control AVP code.</summary>
    public const uint CreditControl = 426;

    /// <summary>The Credit-Control-Failure-Handling AVP code.</summary>
    public const uint CreditControlFailureHandling = 427;

    /// <summary>The Direct-Debiting-Failure-Handling AVP code.</summary>
    public const uint DirectDebitingFailureHandling = 428;

    /// <summary>The Exponent AVP code.</summary>
    public const uint Exponent = 429;

    /// <summary>The Final-Unit-Indication AVP code.</summary>
    public const uint FinalUnitIndication = 430;

    /// <summary>The Granted-Service-Unit AVP code.</summary>
    public const uint GrantedServiceUnit = 431;

    /// <summary>The Rating-Group AVP code.</summary>
    public const uint RatingGroup = 432;

    /// <summary>The Redirect-Address-Type AVP code.</summary>
    public const uint RedirectAddressType = 433;

    /// <summary>The Redirect-Server AVP code.</summary>
    public const uint RedirectServer = 434;

    /// <summary>The Redirect-Server-Address AVP code.</summary>
    public const uint RedirectServerAddress = 435;

    /// <summary>The Requested-Action AVP code.</summary>
    public const uint RequestedAction = 436;

    /// <summary>The Requested-Service-Unit AVP code.</summary>
    public const uint RequestedServiceUnit = 437;

    /// <summary>The Restriction-Filter-Rule AVP code.</summary>
    public const uint RestrictionFilterRule = 438;

    /// <summary>The Service-Identifier AVP code.</summary>
    public const uint ServiceIdentifier = 439;

    /// <summary>The Service-Parameter-Info AVP code.</summary>
    public const uint ServiceParameterInfo = 440;

    /// <summary>The Service-Parameter-Type AVP code.</summary>
    public const uint ServiceParameterType = 441;

    /// <summary>The Service-Parameter-Value AVP code.</summary>
    public const uint ServiceParameterValue = 442;

    /// <summary>The Subscription-Id AVP code.</summary>
    public const uint SubscriptionId = 443;

    /// <summary>The Subscription-Id-Data AVP code.</summary>
    public const uint SubscriptionIdData = 444;

    /// <summary>The Unit-Value AVP code.</summary>
    public const uint UnitValue = 445;

    /// <summary>The Used-Service-Unit AVP code.</summary>
    public const uint UsedServiceUnit = 446;

    /// <summary>The Value-Digits AVP code.</summary>
    public const uint ValueDigits = 447;

    /// <summary>The Validity-Time AVP code.</summary>
    public const uint ValidityTime = 448;

    /// <summary>The Final-Unit-Action AVP code.</summary>
    public const uint FinalUnitAction = 449;

    /// <summary>The Subscription-Id-Type AVP code.</summary>
    public const uint SubscriptionIdType = 450;

    /// <summary>The Tariff-Time-Change AVP code.</summary>
    public const uint TariffTimeChange = 451;

    /// <summary>The Tariff-Change-Usage AVP code.</summary>
    public const uint TariffChangeUsage = 452;

    /// <summary>The G-S-U-Pool-Identifier AVP code.</summary>
    public const uint GsuPoolIdentifier = 453;

    /// <summary>The CC-Unit-Type AVP code.</summary>
    public const uint CcUnitType = 454;

    /// <summary>The Multiple-Services-Indicator AVP code.</summary>
    public const uint MultipleServicesIndicator = 455;

    /// <summary>The Multiple-Services-Credit-Control AVP code.</summary>
    public const uint MultipleServicesCreditControl = 456;

    /// <summary>The G-S-U-Pool-Reference AVP code.</summary>
    public const uint GsuPoolReference = 457;

    /// <summary>The User-Equipment-Info AVP code.</summary>
    public const uint UserEquipmentInfo = 458;

    /// <summary>The User-Equipment-Info-Type AVP code.</summary>
    public const uint UserEquipmentInfoType = 459;

    /// <summary>The User-Equipment-Info-Value AVP code.</summary>
    public const uint UserEquipmentInfoValue = 460;

    /// <summary>The Service-Context-Id AVP code.</summary>
    public const uint ServiceContextId = 461;

    /// <summary>The Accounting-Record-Type AVP code.</summary>
    public const uint AccountingRecordType = 480;

    /// <summary>The Accounting-Realtime-Required AVP code.</summary>
    public const uint AccountingRealtimeRequired = 483;

    /// <summary>The Accounting-Record-Number AVP code.</summary>
    public const uint AccountingRecordNumber = 485;

    /// <summary>The Filter-Rule AVP code reused by credit control.</summary>
    public const uint FilterRule = 509;

    /// <summary>The Redirect-Realm AVP code.</summary>
    public const uint RedirectRealm = 620;

    /// <summary>The OC-Supported-Features AVP code.</summary>
    public const uint OcSupportedFeatures = 621;

    /// <summary>The OC-Feature-Vector AVP code.</summary>
    public const uint OcFeatureVector = 622;

    /// <summary>The OC-OLR AVP code.</summary>
    public const uint OcOlr = 623;

    /// <summary>The OC-Sequence-Number AVP code.</summary>
    public const uint OcSequenceNumber = 624;

    /// <summary>The OC-Validity-Duration AVP code.</summary>
    public const uint OcValidityDuration = 625;

    /// <summary>The OC-Report-Type AVP code.</summary>
    public const uint OcReportType = 626;

    /// <summary>The OC-Reduction-Percentage AVP code.</summary>
    public const uint OcReductionPercentage = 627;

    /// <summary>The OC-Peer-Algo AVP code.</summary>
    public const uint OcPeerAlgorithm = 648;

    /// <summary>The SourceID AVP code.</summary>
    public const uint SourceId = 649;

    /// <summary>The Load AVP code.</summary>
    public const uint Load = 650;

    /// <summary>The Load-Type AVP code.</summary>
    public const uint LoadType = 651;

    /// <summary>The Load-Value AVP code.</summary>
    public const uint LoadValue = 652;

    /// <summary>The User-Equipment-Info-Extension AVP code.</summary>
    public const uint UserEquipmentInfoExtension = 653;

    /// <summary>The User-Equipment-Info-IMEISV AVP code.</summary>
    public const uint UserEquipmentInfoImeisv = 654;

    /// <summary>The User-Equipment-Info-MAC AVP code.</summary>
    public const uint UserEquipmentInfoMac = 655;

    /// <summary>The User-Equipment-Info-EUI64 AVP code.</summary>
    public const uint UserEquipmentInfoEui64 = 656;

    /// <summary>The User-Equipment-Info-ModifiedEUI64 AVP code.</summary>
    public const uint UserEquipmentInfoModifiedEui64 = 657;

    /// <summary>The User-Equipment-Info-IMEI AVP code.</summary>
    public const uint UserEquipmentInfoImei = 658;

    /// <summary>The Subscription-Id-Extension AVP code.</summary>
    public const uint SubscriptionIdExtension = 659;

    /// <summary>The Subscription-Id-E164 AVP code.</summary>
    public const uint SubscriptionIdE164 = 660;

    /// <summary>The Subscription-Id-IMSI AVP code.</summary>
    public const uint SubscriptionIdImsi = 661;

    /// <summary>The Subscription-Id-SIP-URI AVP code.</summary>
    public const uint SubscriptionIdSipUri = 662;

    /// <summary>The Subscription-Id-NAI AVP code.</summary>
    public const uint SubscriptionIdNai = 663;

    /// <summary>The Subscription-Id-Private AVP code.</summary>
    public const uint SubscriptionIdPrivate = 664;

    /// <summary>The Redirect-Server-Extension AVP code.</summary>
    public const uint RedirectServerExtension = 665;

    /// <summary>The Redirect-Address-IPAddress AVP code.</summary>
    public const uint RedirectAddressIpAddress = 666;

    /// <summary>The Redirect-Address-URL AVP code.</summary>
    public const uint RedirectAddressUrl = 667;

    /// <summary>The Redirect-Address-SIP-URI AVP code.</summary>
    public const uint RedirectAddressSipUri = 668;

    /// <summary>The QoS-Final-Unit-Indication AVP code.</summary>
    public const uint QosFinalUnitIndication = 669;

    /// <summary>The OC-Maximum-Rate AVP code.</summary>
    public const uint OcMaximumRate = 670;
}
