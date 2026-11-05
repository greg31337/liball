using System;
using System.Reflection;

namespace diameter.Tests;

public class ConstantContractTests
{
    [Test]
    public void ProtocolConstantsExposeStandardValuesAndTypes()
    {
        AssertPublicFieldCount(typeof(DiameterProtocolConstants), 24);
        AssertConstant(typeof(DiameterProtocolConstants), "Version", typeof(byte), (byte)1);
        AssertConstant(typeof(DiameterProtocolConstants), "MessageHeaderSize", typeof(int), 20);
        AssertConstant(typeof(DiameterProtocolConstants), "AvpHeaderSize", typeof(int), 8);
        AssertConstant(typeof(DiameterProtocolConstants), "VendorSpecificAvpHeaderSize", typeof(int), 12);
        AssertConstant(typeof(DiameterProtocolConstants), "UInt24Size", typeof(int), 3);
        AssertConstant(typeof(DiameterProtocolConstants), "IdentifierSize", typeof(int), 4);
        AssertConstant(typeof(DiameterProtocolConstants), "Alignment", typeof(int), 4);
        AssertConstant(typeof(DiameterProtocolConstants), "MinimumMessageLength", typeof(int), 20);
        AssertConstant(typeof(DiameterProtocolConstants), "MaximumMessageLength", typeof(int), 16777212);
        AssertConstant(typeof(DiameterProtocolConstants), "MaximumAvpLength", typeof(int), 16777215);
        AssertConstant(typeof(DiameterProtocolConstants), "MaximumCommandCode", typeof(uint), 16777215U);
        AssertConstant(typeof(DiameterProtocolConstants), "ExperimentalCommandCodeMinimum", typeof(uint), 16777214U);
        AssertConstant(typeof(DiameterProtocolConstants), "Port", typeof(int), 3868);
        AssertConstant(typeof(DiameterProtocolConstants), "SecurePort", typeof(int), 5868);
        AssertConstant(typeof(DiameterProtocolConstants), "SctpPayloadProtocolIdentifier", typeof(uint), 46U);
        AssertConstant(typeof(DiameterProtocolConstants), "DtlsSctpPayloadProtocolIdentifier", typeof(uint), 47U);
        AssertConstant(typeof(DiameterProtocolConstants), "MessageFlagMask", typeof(byte), (byte)240);
        AssertConstant(typeof(DiameterProtocolConstants), "MessageReservedFlagMask", typeof(byte), (byte)15);
        AssertConstant(typeof(DiameterProtocolConstants), "AvpFlagMask", typeof(byte), (byte)224);
        AssertConstant(typeof(DiameterProtocolConstants), "AvpReservedFlagMask", typeof(byte), (byte)31);
        AssertConstant(typeof(DiameterProtocolConstants), "NtpUnixEpochOffsetSeconds", typeof(long), 2208988800L);
        AssertConstant(typeof(DiameterProtocolConstants), "NtpEraSeconds", typeof(long), 4294967296L);
        AssertConstant(typeof(DiameterProtocolConstants), "EndToEndIdentifierLifetimeSeconds", typeof(int), 240);
        AssertConstant(typeof(DiameterProtocolConstants), "RecommendedConnectionRetrySeconds", typeof(int), 30);
    }

    [Test]
    public void ApplicationAndCommandConstantsExposeRegisteredValues()
    {
        AssertPublicFieldCount(typeof(DiameterApplicationIds), 9);
        AssertUIntConstant(typeof(DiameterApplicationIds), "Common", 0);
        AssertUIntConstant(typeof(DiameterApplicationIds), "BaseAccounting", 3);
        AssertUIntConstant(typeof(DiameterApplicationIds), "CreditControl", 4);
        AssertUIntConstant(typeof(DiameterApplicationIds), "CapabilitiesUpdate", 10);
        AssertUIntConstant(typeof(DiameterApplicationIds), "RealmBasedRedirect", 0);
        AssertUIntConstant(typeof(DiameterApplicationIds), "OverloadControl", 0);
        AssertUIntConstant(typeof(DiameterApplicationIds), "RoutingMessagePriority", 0);
        AssertUIntConstant(typeof(DiameterApplicationIds), "Load", 0);
        AssertUIntConstant(typeof(DiameterApplicationIds), "Relay", uint.MaxValue);

        AssertPublicFieldCount(typeof(DiameterCommandCodes), 9);
        AssertUIntConstant(typeof(DiameterCommandCodes), "CapabilitiesExchange", 257);
        AssertUIntConstant(typeof(DiameterCommandCodes), "ReAuth", 258);
        AssertUIntConstant(typeof(DiameterCommandCodes), "Accounting", 271);
        AssertUIntConstant(typeof(DiameterCommandCodes), "CreditControl", 272);
        AssertUIntConstant(typeof(DiameterCommandCodes), "AbortSession", 274);
        AssertUIntConstant(typeof(DiameterCommandCodes), "SessionTermination", 275);
        AssertUIntConstant(typeof(DiameterCommandCodes), "DeviceWatchdog", 280);
        AssertUIntConstant(typeof(DiameterCommandCodes), "DisconnectPeer", 282);
        AssertUIntConstant(typeof(DiameterCommandCodes), "CapabilitiesUpdate", 328);
    }

    [Test]
    public void BaseAndAccountingAvpConstantsExposeRegisteredValues()
    {
        AssertPublicFieldCount(typeof(DiameterAvpCodes), 136);
        AssertUIntConstant(typeof(DiameterAvpCodes), "UserName", 1);
        AssertUIntConstant(typeof(DiameterAvpCodes), "FilterId", 11);
        AssertUIntConstant(typeof(DiameterAvpCodes), "Class", 25);
        AssertUIntConstant(typeof(DiameterAvpCodes), "SessionTimeout", 27);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ProxyState", 33);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AcctSessionId", 44);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AcctMultiSessionId", 50);
        AssertUIntConstant(typeof(DiameterAvpCodes), "EventTimestamp", 55);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AcctInterimInterval", 85);
        AssertUIntConstant(typeof(DiameterAvpCodes), "HostIpAddress", 257);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AuthApplicationId", 258);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AcctApplicationId", 259);
        AssertUIntConstant(typeof(DiameterAvpCodes), "VendorSpecificApplicationId", 260);
        AssertUIntConstant(typeof(DiameterAvpCodes), "RedirectHostUsage", 261);
        AssertUIntConstant(typeof(DiameterAvpCodes), "RedirectMaxCacheTime", 262);
        AssertUIntConstant(typeof(DiameterAvpCodes), "SessionId", 263);
        AssertUIntConstant(typeof(DiameterAvpCodes), "OriginHost", 264);
        AssertUIntConstant(typeof(DiameterAvpCodes), "SupportedVendorId", 265);
        AssertUIntConstant(typeof(DiameterAvpCodes), "VendorId", 266);
        AssertUIntConstant(typeof(DiameterAvpCodes), "FirmwareRevision", 267);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ResultCode", 268);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ProductName", 269);
        AssertUIntConstant(typeof(DiameterAvpCodes), "SessionBinding", 270);
        AssertUIntConstant(typeof(DiameterAvpCodes), "SessionServerFailover", 271);
        AssertUIntConstant(typeof(DiameterAvpCodes), "MultiRoundTimeOut", 272);
        AssertUIntConstant(typeof(DiameterAvpCodes), "DisconnectCause", 273);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AuthRequestType", 274);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AuthGracePeriod", 276);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AuthSessionState", 277);
        AssertUIntConstant(typeof(DiameterAvpCodes), "OriginStateId", 278);
        AssertUIntConstant(typeof(DiameterAvpCodes), "FailedAvp", 279);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ProxyHost", 280);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ErrorMessage", 281);
        AssertUIntConstant(typeof(DiameterAvpCodes), "RouteRecord", 282);
        AssertUIntConstant(typeof(DiameterAvpCodes), "DestinationRealm", 283);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ProxyInfo", 284);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ReAuthRequestType", 285);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AccountingSubSessionId", 287);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AuthorizationLifetime", 291);
        AssertUIntConstant(typeof(DiameterAvpCodes), "RedirectHost", 292);
        AssertUIntConstant(typeof(DiameterAvpCodes), "DestinationHost", 293);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ErrorReportingHost", 294);
        AssertUIntConstant(typeof(DiameterAvpCodes), "TerminationCause", 295);
        AssertUIntConstant(typeof(DiameterAvpCodes), "OriginRealm", 296);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ExperimentalResult", 297);
        AssertUIntConstant(typeof(DiameterAvpCodes), "ExperimentalResultCode", 298);
        AssertUIntConstant(typeof(DiameterAvpCodes), "InbandSecurityId", 299);
        AssertUIntConstant(typeof(DiameterAvpCodes), "E2ESequence", 300);
        AssertUIntConstant(typeof(DiameterAvpCodes), "Drmp", 301);
        AssertUIntConstant(typeof(DiameterAvpCodes), "QosFilterRule", 407);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AccountingRecordType", 480);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AccountingRealtimeRequired", 483);
        AssertUIntConstant(typeof(DiameterAvpCodes), "AccountingRecordNumber", 485);
        AssertUIntConstant(typeof(DiameterAvpCodes), "FilterRule", 509);
    }

    [Test]
    public void CreditControlAndExtensionAvpConstantsExposeRegisteredValues()
    {
        AssertSequentialUIntConstants(
            typeof(DiameterAvpCodes),
            411,
            new string[]
            {
                "CcCorrelationId", "CcInputOctets", "CcMoney", "CcOutputOctets",
                "CcRequestNumber", "CcRequestType", "CcServiceSpecificUnits",
                "CcSessionFailover", "CcSubSessionId", "CcTime", "CcTotalOctets",
                "CheckBalanceResult", "CostInformation", "CostUnit", "CurrencyCode",
                "CreditControl", "CreditControlFailureHandling",
                "DirectDebitingFailureHandling", "Exponent", "FinalUnitIndication",
                "GrantedServiceUnit", "RatingGroup", "RedirectAddressType",
                "RedirectServer", "RedirectServerAddress", "RequestedAction",
                "RequestedServiceUnit", "RestrictionFilterRule", "ServiceIdentifier",
                "ServiceParameterInfo", "ServiceParameterType", "ServiceParameterValue",
                "SubscriptionId", "SubscriptionIdData", "UnitValue", "UsedServiceUnit",
                "ValueDigits", "ValidityTime", "FinalUnitAction", "SubscriptionIdType",
                "TariffTimeChange", "TariffChangeUsage", "GsuPoolIdentifier", "CcUnitType",
                "MultipleServicesIndicator", "MultipleServicesCreditControl",
                "GsuPoolReference", "UserEquipmentInfo", "UserEquipmentInfoType",
                "UserEquipmentInfoValue", "ServiceContextId"
            });
        AssertSequentialUIntConstants(
            typeof(DiameterAvpCodes),
            620,
            new string[]
            {
                "RedirectRealm", "OcSupportedFeatures", "OcFeatureVector", "OcOlr",
                "OcSequenceNumber", "OcValidityDuration", "OcReportType",
                "OcReductionPercentage"
            });
        AssertSequentialUIntConstants(
            typeof(DiameterAvpCodes),
            648,
            new string[]
            {
                "OcPeerAlgorithm", "SourceId", "Load", "LoadType", "LoadValue",
                "UserEquipmentInfoExtension", "UserEquipmentInfoImeisv",
                "UserEquipmentInfoMac", "UserEquipmentInfoEui64",
                "UserEquipmentInfoModifiedEui64", "UserEquipmentInfoImei",
                "SubscriptionIdExtension", "SubscriptionIdE164", "SubscriptionIdImsi",
                "SubscriptionIdSipUri", "SubscriptionIdNai", "SubscriptionIdPrivate",
                "RedirectServerExtension", "RedirectAddressIpAddress",
                "RedirectAddressUrl", "RedirectAddressSipUri", "QosFinalUnitIndication",
                "OcMaximumRate"
            });
    }

    [Test]
    public void ResultCodeConstantsExposeStandardValues()
    {
        AssertPublicFieldCount(typeof(DiameterResultCodes), 39);
        AssertUIntConstant(typeof(DiameterResultCodes), "MultiRoundAuth", 1001);
        AssertUIntConstant(typeof(DiameterResultCodes), "Success", 2001);
        AssertUIntConstant(typeof(DiameterResultCodes), "LimitedSuccess", 2002);
        AssertUIntConstant(typeof(DiameterResultCodes), "CommandUnsupported", 3001);
        AssertUIntConstant(typeof(DiameterResultCodes), "UnableToDeliver", 3002);
        AssertUIntConstant(typeof(DiameterResultCodes), "RealmNotServed", 3003);
        AssertUIntConstant(typeof(DiameterResultCodes), "TooBusy", 3004);
        AssertUIntConstant(typeof(DiameterResultCodes), "LoopDetected", 3005);
        AssertUIntConstant(typeof(DiameterResultCodes), "RedirectIndication", 3006);
        AssertUIntConstant(typeof(DiameterResultCodes), "ApplicationUnsupported", 3007);
        AssertUIntConstant(typeof(DiameterResultCodes), "InvalidHeaderBits", 3008);
        AssertUIntConstant(typeof(DiameterResultCodes), "InvalidAvpBits", 3009);
        AssertUIntConstant(typeof(DiameterResultCodes), "UnknownPeer", 3010);
        AssertUIntConstant(typeof(DiameterResultCodes), "RealmRedirectIndication", 3011);
        AssertUIntConstant(typeof(DiameterResultCodes), "AuthenticationRejected", 4001);
        AssertUIntConstant(typeof(DiameterResultCodes), "OutOfSpace", 4002);
        AssertUIntConstant(typeof(DiameterResultCodes), "ElectionLost", 4003);
        AssertUIntConstant(typeof(DiameterResultCodes), "EndUserServiceDenied", 4010);
        AssertUIntConstant(typeof(DiameterResultCodes), "CreditControlNotApplicable", 4011);
        AssertUIntConstant(typeof(DiameterResultCodes), "CreditLimitReached", 4012);
        AssertUIntConstant(typeof(DiameterResultCodes), "AvpUnsupported", 5001);
        AssertUIntConstant(typeof(DiameterResultCodes), "UnknownSessionId", 5002);
        AssertUIntConstant(typeof(DiameterResultCodes), "AuthorizationRejected", 5003);
        AssertUIntConstant(typeof(DiameterResultCodes), "InvalidAvpValue", 5004);
        AssertUIntConstant(typeof(DiameterResultCodes), "MissingAvp", 5005);
        AssertUIntConstant(typeof(DiameterResultCodes), "ResourcesExceeded", 5006);
        AssertUIntConstant(typeof(DiameterResultCodes), "ContradictingAvps", 5007);
        AssertUIntConstant(typeof(DiameterResultCodes), "AvpNotAllowed", 5008);
        AssertUIntConstant(typeof(DiameterResultCodes), "AvpOccursTooManyTimes", 5009);
        AssertUIntConstant(typeof(DiameterResultCodes), "NoCommonApplication", 5010);
        AssertUIntConstant(typeof(DiameterResultCodes), "UnsupportedVersion", 5011);
        AssertUIntConstant(typeof(DiameterResultCodes), "UnableToComply", 5012);
        AssertUIntConstant(typeof(DiameterResultCodes), "InvalidBitInHeader", 5013);
        AssertUIntConstant(typeof(DiameterResultCodes), "InvalidAvpLength", 5014);
        AssertUIntConstant(typeof(DiameterResultCodes), "InvalidMessageLength", 5015);
        AssertUIntConstant(typeof(DiameterResultCodes), "InvalidAvpBitCombination", 5016);
        AssertUIntConstant(typeof(DiameterResultCodes), "NoCommonSecurity", 5017);
        AssertUIntConstant(typeof(DiameterResultCodes), "UserUnknown", 5030);
        AssertUIntConstant(typeof(DiameterResultCodes), "RatingFailed", 5031);
    }

    [Test]
    public void VendorConstantsExposeEnterpriseNumbers()
    {
        AssertPublicFieldCount(typeof(DiameterVendorIds), 5);
        AssertUIntConstant(typeof(DiameterVendorIds), "Ietf", 0);
        AssertUIntConstant(typeof(DiameterVendorIds), "CableLabs", 4491);
        AssertUIntConstant(typeof(DiameterVendorIds), "ThreeGpp2", 5535);
        AssertUIntConstant(typeof(DiameterVendorIds), "ThreeGpp", 10415);
        AssertUIntConstant(typeof(DiameterVendorIds), "Etsi", 13019);
    }

    private static void AssertPublicFieldCount(Type type, int expectedCount)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        Assert.That(fields, Has.Length.EqualTo(expectedCount));
    }

    private static void AssertUIntConstant(Type type, string name, uint expectedValue)
    {
        AssertConstant(type, name, typeof(uint), expectedValue);
    }

    private static void AssertSequentialUIntConstants(Type type, uint firstValue, string[] names)
    {
        for (int index = 0; index < names.Length; index++)
        {
            AssertUIntConstant(type, names[index], firstValue + (uint)index);
        }
    }

    private static void AssertConstant(Type type, string name, Type expectedType, object expectedValue)
    {
        FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, $"Missing public constant {type.Name}.{name}");
        if (field == null)
        {
            return;
        }

        Assert.That(field.FieldType, Is.EqualTo(expectedType));
        Assert.That(field.GetValue(null), Is.EqualTo(expectedValue));
    }
}
