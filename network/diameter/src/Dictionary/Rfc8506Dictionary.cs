using System.Collections.Generic;

namespace diameter;

internal static class Rfc8506Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc8506";
        module.Version = "RFC 8506";
        module.Dependencies.Add("rfc6733");
        AddApplication(module);
        AddTypes(module);
        AddEnumValues(module);
        AddAvps(module);
        AddCommands(module);
        return module;
    }

    internal static void AddApplication(DiameterDictionaryModule module)
    {
        DiameterApplicationDefinition application = new DiameterApplicationDefinition();
        application.Id = DiameterApplicationIds.CreditControl;
        application.VendorId = DiameterVendorIds.Ietf;
        application.Name = "Diameter Credit Control";
        module.Applications.Add(application);
    }

    internal static void AddTypes(DiameterDictionaryModule module)
    {
        module.Types.Add(CreateEnumeratedType("CC-Request-Type"));
        module.Types.Add(CreateEnumeratedType("CC-Session-Failover"));
        module.Types.Add(CreateEnumeratedType("Check-Balance-Result"));
        module.Types.Add(CreateEnumeratedType("Credit-Control"));
        module.Types.Add(CreateEnumeratedType("Credit-Control-Failure-Handling"));
        module.Types.Add(CreateEnumeratedType("Direct-Debiting-Failure-Handling"));
        module.Types.Add(CreateEnumeratedType("Tariff-Change-Usage"));
        module.Types.Add(CreateEnumeratedType("CC-Unit-Type"));
        module.Types.Add(CreateEnumeratedType("Final-Unit-Action"));
        module.Types.Add(CreateEnumeratedType("Redirect-Address-Type"));
        module.Types.Add(CreateEnumeratedType("Multiple-Services-Indicator"));
        module.Types.Add(CreateEnumeratedType("Requested-Action"));
        module.Types.Add(CreateEnumeratedType("Subscription-Id-Type"));
        module.Types.Add(CreateEnumeratedType("User-Equipment-Info-Type"));
        module.Types.Add(Rfc6733Dictionary.CreateType(
            "QoSFilterRule",
            DiameterAvpBaseType.OctetString,
            "OctetString",
            -1));
    }

    internal static void AddEnumValues(DiameterDictionaryModule module)
    {
        AddEnumDomain(
            module,
            "CC-Request-Type",
            DiameterAvpCodes.CcRequestType,
            new string[]
            {
                "INITIAL_REQUEST", "UPDATE_REQUEST", "TERMINATION_REQUEST", "EVENT_REQUEST"
            },
            new int[] { 1, 2, 3, 4 });
        AddEnumDomain(
            module,
            "CC-Session-Failover",
            DiameterAvpCodes.CcSessionFailover,
            new string[] { "FAILOVER_NOT_SUPPORTED", "FAILOVER_SUPPORTED" },
            new int[] { 0, 1 });
        AddEnumDomain(
            module,
            "Check-Balance-Result",
            DiameterAvpCodes.CheckBalanceResult,
            new string[] { "ENOUGH_CREDIT", "NO_CREDIT" },
            new int[] { 0, 1 });
        AddEnumDomain(
            module,
            "Credit-Control",
            DiameterAvpCodes.CreditControl,
            new string[] { "CREDIT_AUTHORIZATION", "RE_AUTHORIZATION" },
            new int[] { 0, 1 });
        AddEnumDomain(
            module,
            "Credit-Control-Failure-Handling",
            DiameterAvpCodes.CreditControlFailureHandling,
            new string[] { "TERMINATE", "CONTINUE", "RETRY_AND_TERMINATE" },
            new int[] { 0, 1, 2 });
        AddEnumDomain(
            module,
            "Direct-Debiting-Failure-Handling",
            DiameterAvpCodes.DirectDebitingFailureHandling,
            new string[] { "TERMINATE_OR_BUFFER", "CONTINUE" },
            new int[] { 0, 1 });
        AddEnumDomain(
            module,
            "Tariff-Change-Usage",
            DiameterAvpCodes.TariffChangeUsage,
            new string[]
            {
                "UNIT_BEFORE_TARIFF_CHANGE", "UNIT_AFTER_TARIFF_CHANGE",
                "UNIT_INDETERMINATE"
            },
            new int[] { 0, 1, 2 });
        AddEnumDomain(
            module,
            "CC-Unit-Type",
            DiameterAvpCodes.CcUnitType,
            new string[]
            {
                "TIME", "MONEY", "TOTAL-OCTETS", "INPUT-OCTETS", "OUTPUT-OCTETS",
                "SERVICE-SPECIFIC-UNITS"
            },
            new int[] { 0, 1, 2, 3, 4, 5 });
        AddEnumDomain(
            module,
            "Final-Unit-Action",
            DiameterAvpCodes.FinalUnitAction,
            new string[] { "TERMINATE", "REDIRECT", "RESTRICT_ACCESS" },
            new int[] { 0, 1, 2 });
        AddEnumDomain(
            module,
            "Redirect-Address-Type",
            DiameterAvpCodes.RedirectAddressType,
            new string[] { "IPV4_ADDRESS", "IPV6_ADDRESS", "URL", "SIP_URI" },
            new int[] { 0, 1, 2, 3 });
        AddEnumDomain(
            module,
            "Multiple-Services-Indicator",
            DiameterAvpCodes.MultipleServicesIndicator,
            new string[]
            {
                "MULTIPLE_SERVICES_NOT_SUPPORTED", "MULTIPLE_SERVICES_SUPPORTED"
            },
            new int[] { 0, 1 });
        AddEnumDomain(
            module,
            "Requested-Action",
            DiameterAvpCodes.RequestedAction,
            new string[]
            {
                "DIRECT_DEBITING", "REFUND_ACCOUNT", "CHECK_BALANCE", "PRICE_ENQUIRY"
            },
            new int[] { 0, 1, 2, 3 });
        AddEnumDomain(
            module,
            "Subscription-Id-Type",
            DiameterAvpCodes.SubscriptionIdType,
            new string[]
            {
                "END_USER_E164", "END_USER_IMSI", "END_USER_SIP_URI", "END_USER_NAI",
                "END_USER_PRIVATE"
            },
            new int[] { 0, 1, 2, 3, 4 });
        AddEnumDomain(
            module,
            "User-Equipment-Info-Type",
            DiameterAvpCodes.UserEquipmentInfoType,
            new string[] { "IMEISV", "MAC", "EUI64", "MODIFIED_EUI64" },
            new int[] { 0, 1, 2, 3 });
        AddResultCodes(module);
    }

    internal static void AddResultCodes(DiameterDictionaryModule module)
    {
        module.EnumValues.Add(CreateValue(
            "DIAMETER_END_USER_SERVICE_DENIED",
            4010,
            "",
            DiameterAvpCodes.ResultCode));
        module.EnumValues.Add(CreateValue(
            "DIAMETER_CREDIT_CONTROL_NOT_APPLICABLE",
            4011,
            "",
            DiameterAvpCodes.ResultCode));
        module.EnumValues.Add(CreateValue(
            "DIAMETER_CREDIT_LIMIT_REACHED",
            4012,
            "",
            DiameterAvpCodes.ResultCode));
        module.EnumValues.Add(CreateValue(
            "DIAMETER_USER_UNKNOWN",
            5030,
            "",
            DiameterAvpCodes.ResultCode));
        module.EnumValues.Add(CreateValue(
            "DIAMETER_RATING_FAILED",
            5031,
            "",
            DiameterAvpCodes.ResultCode));
    }

    internal static void AddEnumDomain(
        DiameterDictionaryModule module,
        string typeName,
        uint avpCode,
        string[] names,
        int[] values)
    {
        int index = 0;
        while (index < names.Length)
        {
            module.EnumValues.Add(CreateValue(
                names[index],
                values[index],
                typeName,
                avpCode));
            index = index + 1;
        }
    }

    internal static void AddAvps(DiameterDictionaryModule module)
    {
        AddScalarAvps(module);
        AddGroupedAvps(module);
    }

    internal static void AddScalarAvps(DiameterDictionaryModule module)
    {
        AddAvp(module, 411U, "CC-Correlation-Id", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 412U, "CC-Input-Octets", "Unsigned64", DiameterAvpBaseType.Unsigned64, true);
        AddAvp(module, 414U, "CC-Output-Octets", "Unsigned64", DiameterAvpBaseType.Unsigned64, true);
        AddAvp(module, 415U, "CC-Request-Number", "Unsigned32", DiameterAvpBaseType.Unsigned32, true);
        AddAvp(module, 416U, "CC-Request-Type", "CC-Request-Type", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 417U, "CC-Service-Specific-Units", "Unsigned64", DiameterAvpBaseType.Unsigned64, true);
        AddAvp(module, 418U, "CC-Session-Failover", "CC-Session-Failover", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 419U, "CC-Sub-Session-Id", "Unsigned64", DiameterAvpBaseType.Unsigned64, true);
        AddAvp(module, 420U, "CC-Time", "Unsigned32", DiameterAvpBaseType.Unsigned32, true);
        AddAvp(module, 421U, "CC-Total-Octets", "Unsigned64", DiameterAvpBaseType.Unsigned64, true);
        AddAvp(module, 422U, "Check-Balance-Result", "Check-Balance-Result", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 424U, "Cost-Unit", "UTF8String", DiameterAvpBaseType.OctetString, true);
        AddAvp(module, 425U, "Currency-Code", "Unsigned32", DiameterAvpBaseType.Unsigned32, true);
        AddAvp(module, 426U, "Credit-Control", "Credit-Control", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 427U, "Credit-Control-Failure-Handling", "Credit-Control-Failure-Handling", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 428U, "Direct-Debiting-Failure-Handling", "Direct-Debiting-Failure-Handling", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 429U, "Exponent", "Integer32", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 432U, "Rating-Group", "Unsigned32", DiameterAvpBaseType.Unsigned32, true);
        AddAvp(module, 433U, "Redirect-Address-Type", "Redirect-Address-Type", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 435U, "Redirect-Server-Address", "UTF8String", DiameterAvpBaseType.OctetString, true);
        AddAvp(module, 436U, "Requested-Action", "Requested-Action", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 438U, "Restriction-Filter-Rule", "IPFilterRule", DiameterAvpBaseType.OctetString, true);
        AddAvp(module, 439U, "Service-Identifier", "Unsigned32", DiameterAvpBaseType.Unsigned32, true);
        AddAvp(module, 441U, "Service-Parameter-Type", "Unsigned32", DiameterAvpBaseType.Unsigned32, false);
        AddAvp(module, 442U, "Service-Parameter-Value", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 444U, "Subscription-Id-Data", "UTF8String", DiameterAvpBaseType.OctetString, true);
        AddAvp(module, 447U, "Value-Digits", "Integer64", DiameterAvpBaseType.Integer64, true);
        AddAvp(module, 448U, "Validity-Time", "Unsigned32", DiameterAvpBaseType.Unsigned32, true);
        AddAvp(module, 449U, "Final-Unit-Action", "Final-Unit-Action", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 450U, "Subscription-Id-Type", "Subscription-Id-Type", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 451U, "Tariff-Time-Change", "Time", DiameterAvpBaseType.OctetString, true);
        AddAvp(module, 452U, "Tariff-Change-Usage", "Tariff-Change-Usage", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 453U, "G-S-U-Pool-Identifier", "Unsigned32", DiameterAvpBaseType.Unsigned32, true);
        AddAvp(module, 454U, "CC-Unit-Type", "CC-Unit-Type", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 455U, "Multiple-Services-Indicator", "Multiple-Services-Indicator", DiameterAvpBaseType.Integer32, true);
        AddAvp(module, 459U, "User-Equipment-Info-Type", "User-Equipment-Info-Type", DiameterAvpBaseType.Integer32, false);
        AddAvp(module, 460U, "User-Equipment-Info-Value", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 461U, "Service-Context-Id", "UTF8String", DiameterAvpBaseType.OctetString, true);
        AddAvp(module, 654U, "User-Equipment-Info-IMEISV", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 655U, "User-Equipment-Info-MAC", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 656U, "User-Equipment-Info-EUI64", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 657U, "User-Equipment-Info-ModifiedEUI64", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 658U, "User-Equipment-Info-IMEI", "OctetString", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 660U, "Subscription-Id-E164", "UTF8String", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 661U, "Subscription-Id-IMSI", "UTF8String", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 662U, "Subscription-Id-SIP-URI", "UTF8String", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 663U, "Subscription-Id-NAI", "UTF8String", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 664U, "Subscription-Id-Private", "UTF8String", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 666U, "Redirect-Address-IPAddress", "Address", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 667U, "Redirect-Address-URL", "UTF8String", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, 668U, "Redirect-Address-SIP-URI", "UTF8String", DiameterAvpBaseType.OctetString, false);
        AddAvp(module, DiameterAvpCodes.FilterId, "Filter-Id", "UTF8String", DiameterAvpBaseType.OctetString, true);
        AddAvp(module, DiameterAvpCodes.FilterRule, "Filter-Rule", "QoSFilterRule", DiameterAvpBaseType.OctetString, true);
    }

    internal static void AddGroupedAvps(DiameterDictionaryModule module)
    {
        DiameterAvpDefinition cost = CreateGrouped(423U, "Cost-Information", true, false);
        AddGroupedRule(cost, 445U, "Unit-Value", 1, 1);
        AddGroupedRule(cost, 425U, "Currency-Code", 1, 1);
        AddGroupedRule(cost, 424U, "Cost-Unit", 0, 1);
        module.Avps.Add(cost);

        DiameterAvpDefinition unitValue = CreateGrouped(445U, "Unit-Value", true, false);
        AddGroupedRule(unitValue, 447U, "Value-Digits", 1, 1);
        AddGroupedRule(unitValue, 429U, "Exponent", 0, 1);
        module.Avps.Add(unitValue);

        DiameterAvpDefinition multiple = CreateGrouped(456U, "Multiple-Services-Credit-Control", true, true);
        AddGroupedRule(multiple, 431U, "Granted-Service-Unit", 0, 1);
        AddGroupedRule(multiple, 437U, "Requested-Service-Unit", 0, 1);
        AddGroupedRule(multiple, 446U, "Used-Service-Unit", 0, -1);
        AddGroupedRule(multiple, 452U, "Tariff-Change-Usage", 0, 1);
        AddGroupedRule(multiple, 439U, "Service-Identifier", 0, -1);
        AddGroupedRule(multiple, 432U, "Rating-Group", 0, 1);
        AddGroupedRule(multiple, 457U, "G-S-U-Pool-Reference", 0, -1);
        AddGroupedRule(multiple, 448U, "Validity-Time", 0, 1);
        AddGroupedRule(multiple, DiameterAvpCodes.ResultCode, "Result-Code", 0, 1);
        AddGroupedRule(multiple, 430U, "Final-Unit-Indication", 0, 1);
        AddGroupedRule(multiple, 669U, "QoS-Final-Unit-Indication", 0, 1);
        module.Avps.Add(multiple);

        module.Avps.Add(CreateServiceUnit(431U, "Granted-Service-Unit", true));
        module.Avps.Add(CreateServiceUnit(437U, "Requested-Service-Unit", false));
        module.Avps.Add(CreateUsedServiceUnit());

        DiameterAvpDefinition money = CreateGrouped(413U, "CC-Money", true, false);
        AddGroupedRule(money, 445U, "Unit-Value", 1, 1);
        AddGroupedRule(money, 425U, "Currency-Code", 0, 1);
        module.Avps.Add(money);

        DiameterAvpDefinition pool = CreateGrouped(457U, "G-S-U-Pool-Reference", true, false);
        AddGroupedRule(pool, 453U, "G-S-U-Pool-Identifier", 1, 1);
        AddGroupedRule(pool, 454U, "CC-Unit-Type", 1, 1);
        AddGroupedRule(pool, 445U, "Unit-Value", 1, 1);
        module.Avps.Add(pool);

        DiameterAvpDefinition finalUnit = CreateGrouped(430U, "Final-Unit-Indication", true, false);
        AddGroupedRule(finalUnit, 449U, "Final-Unit-Action", 1, 1);
        AddGroupedRule(finalUnit, 438U, "Restriction-Filter-Rule", 0, -1);
        AddGroupedRule(finalUnit, DiameterAvpCodes.FilterId, "Filter-Id", 0, -1);
        AddGroupedRule(finalUnit, 434U, "Redirect-Server", 0, 1);
        module.Avps.Add(finalUnit);

        DiameterAvpDefinition redirect = CreateGrouped(434U, "Redirect-Server", true, false);
        AddGroupedRule(redirect, 433U, "Redirect-Address-Type", 1, 1);
        AddGroupedRule(redirect, 435U, "Redirect-Server-Address", 1, 1);
        module.Avps.Add(redirect);

        DiameterAvpDefinition parameter = CreateGrouped(440U, "Service-Parameter-Info", false, false);
        AddGroupedRule(parameter, 441U, "Service-Parameter-Type", 1, 1);
        AddGroupedRule(parameter, 442U, "Service-Parameter-Value", 1, 1);
        module.Avps.Add(parameter);

        DiameterAvpDefinition subscription = CreateGrouped(443U, "Subscription-Id", true, false);
        AddGroupedRule(subscription, 450U, "Subscription-Id-Type", 1, 1);
        AddGroupedRule(subscription, 444U, "Subscription-Id-Data", 1, 1);
        module.Avps.Add(subscription);

        DiameterAvpDefinition equipment = CreateGrouped(458U, "User-Equipment-Info", false, false);
        AddGroupedRule(equipment, 459U, "User-Equipment-Info-Type", 1, 1);
        AddGroupedRule(equipment, 460U, "User-Equipment-Info-Value", 1, 1);
        module.Avps.Add(equipment);

        AddExtensionGroupedAvps(module);
    }

    internal static DiameterAvpDefinition CreateServiceUnit(
        uint code,
        string name,
        bool includeTariffTime)
    {
        DiameterAvpDefinition avp = CreateGrouped(code, name, true, true);
        if (includeTariffTime)
        {
            AddGroupedRule(avp, 451U, "Tariff-Time-Change", 0, 1);
        }

        AddServiceUnitRules(avp);
        return avp;
    }

    internal static DiameterAvpDefinition CreateUsedServiceUnit()
    {
        DiameterAvpDefinition avp = CreateGrouped(446U, "Used-Service-Unit", true, true);
        AddGroupedRule(avp, 452U, "Tariff-Change-Usage", 0, 1);
        AddServiceUnitRules(avp);
        return avp;
    }

    internal static void AddServiceUnitRules(DiameterAvpDefinition avp)
    {
        AddGroupedRule(avp, 420U, "CC-Time", 0, 1);
        AddGroupedRule(avp, 413U, "CC-Money", 0, 1);
        AddGroupedRule(avp, 421U, "CC-Total-Octets", 0, 1);
        AddGroupedRule(avp, 412U, "CC-Input-Octets", 0, 1);
        AddGroupedRule(avp, 414U, "CC-Output-Octets", 0, 1);
        AddGroupedRule(avp, 417U, "CC-Service-Specific-Units", 0, 1);
    }

    internal static void AddExtensionGroupedAvps(DiameterDictionaryModule module)
    {
        DiameterAvpDefinition equipment = CreateGrouped(653U, "User-Equipment-Info-Extension", false, true);
        AddGroupedRule(equipment, 654U, "User-Equipment-Info-IMEISV", 0, 1);
        AddGroupedRule(equipment, 655U, "User-Equipment-Info-MAC", 0, 1);
        AddGroupedRule(equipment, 656U, "User-Equipment-Info-EUI64", 0, 1);
        AddGroupedRule(equipment, 657U, "User-Equipment-Info-ModifiedEUI64", 0, 1);
        AddGroupedRule(equipment, 658U, "User-Equipment-Info-IMEI", 0, 1);
        module.Avps.Add(equipment);

        DiameterAvpDefinition subscription = CreateGrouped(659U, "Subscription-Id-Extension", false, true);
        AddGroupedRule(subscription, 660U, "Subscription-Id-E164", 0, 1);
        AddGroupedRule(subscription, 661U, "Subscription-Id-IMSI", 0, 1);
        AddGroupedRule(subscription, 662U, "Subscription-Id-SIP-URI", 0, 1);
        AddGroupedRule(subscription, 663U, "Subscription-Id-NAI", 0, 1);
        AddGroupedRule(subscription, 664U, "Subscription-Id-Private", 0, 1);
        module.Avps.Add(subscription);

        DiameterAvpDefinition redirect = CreateGrouped(665U, "Redirect-Server-Extension", false, true);
        AddGroupedRule(redirect, 666U, "Redirect-Address-IPAddress", 0, 1);
        AddGroupedRule(redirect, 667U, "Redirect-Address-URL", 0, 1);
        AddGroupedRule(redirect, 668U, "Redirect-Address-SIP-URI", 0, 1);
        module.Avps.Add(redirect);

        DiameterAvpDefinition finalUnit = CreateGrouped(669U, "QoS-Final-Unit-Indication", false, true);
        AddGroupedRule(finalUnit, 449U, "Final-Unit-Action", 1, 1);
        AddGroupedRule(finalUnit, DiameterAvpCodes.FilterRule, "Filter-Rule", 0, -1);
        AddGroupedRule(finalUnit, DiameterAvpCodes.FilterId, "Filter-Id", 0, -1);
        AddGroupedRule(finalUnit, 665U, "Redirect-Server-Extension", 0, 1);
        module.Avps.Add(finalUnit);
    }

    internal static void AddCommands(DiameterDictionaryModule module)
    {
        AddCreditControlCommands(module);
        AddReAuthCommands(module);
    }

    internal static void AddCreditControlCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = Rfc6733Dictionary.CreateCommand(
            DiameterApplicationIds.CreditControl,
            DiameterCommandCodes.CreditControl,
            "Credit-Control",
            true,
            true);
        AddCreditControlRequestRules(request);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = Rfc6733Dictionary.CreateCommand(
            DiameterApplicationIds.CreditControl,
            DiameterCommandCodes.CreditControl,
            "Credit-Control",
            false,
            true);
        AddCreditControlAnswerRules(answer);
        module.Commands.Add(answer);
    }

    internal static void AddCreditControlRequestRules(DiameterCommandDefinition command)
    {
        AddCommandRule(command, 263U, "Session-Id", 1, 1, 0);
        AddCommandRule(command, 264U, "Origin-Host", 1, 1, -1);
        AddCommandRule(command, 296U, "Origin-Realm", 1, 1, -1);
        AddCommandRule(command, 283U, "Destination-Realm", 1, 1, -1);
        AddCommandRule(command, 258U, "Auth-Application-Id", 1, 1, -1);
        AddCommandRule(command, 461U, "Service-Context-Id", 1, 1, -1);
        AddCommandRule(command, 416U, "CC-Request-Type", 1, 1, -1);
        AddCommandRule(command, 415U, "CC-Request-Number", 1, 1, -1);
        AddCommandRule(command, 293U, "Destination-Host", 0, 1, -1);
        AddCommandRule(command, 1U, "User-Name", 0, 1, -1);
        AddCommandRule(command, 419U, "CC-Sub-Session-Id", 0, 1, -1);
        AddCommandRule(command, 50U, "Acct-Multi-Session-Id", 0, 1, -1);
        AddCommandRule(command, 278U, "Origin-State-Id", 0, 1, -1);
        AddCommandRule(command, 55U, "Event-Timestamp", 0, 1, -1);
        AddCommandRule(command, 443U, "Subscription-Id", 0, -1, -1);
        AddCommandRule(command, 659U, "Subscription-Id-Extension", 0, -1, -1);
        AddCommandRule(command, 439U, "Service-Identifier", 0, 1, -1);
        AddCommandRule(command, 295U, "Termination-Cause", 0, 1, -1);
        AddCommandRule(command, 437U, "Requested-Service-Unit", 0, 1, -1);
        AddCommandRule(command, 436U, "Requested-Action", 0, 1, -1);
        AddCommandRule(command, 446U, "Used-Service-Unit", 0, -1, -1);
        AddCommandRule(command, 455U, "Multiple-Services-Indicator", 0, 1, -1);
        AddCommandRule(command, 456U, "Multiple-Services-Credit-Control", 0, -1, -1);
        AddCommandRule(command, 440U, "Service-Parameter-Info", 0, -1, -1);
        AddCommandRule(command, 411U, "CC-Correlation-Id", 0, 1, -1);
        AddCommandRule(command, 458U, "User-Equipment-Info", 0, 1, -1);
        AddCommandRule(command, 653U, "User-Equipment-Info-Extension", 0, 1, -1);
        AddCommandRule(command, 284U, "Proxy-Info", 0, -1, -1);
        AddCommandRule(command, 282U, "Route-Record", 0, -1, -1);
    }

    internal static void AddCreditControlAnswerRules(DiameterCommandDefinition command)
    {
        AddCommandRule(command, 263U, "Session-Id", 1, 1, 0);
        AddCommandRule(command, 268U, "Result-Code", 1, 1, -1);
        AddCommandRule(command, 264U, "Origin-Host", 1, 1, -1);
        AddCommandRule(command, 296U, "Origin-Realm", 1, 1, -1);
        AddCommandRule(command, 258U, "Auth-Application-Id", 1, 1, -1);
        AddCommandRule(command, 416U, "CC-Request-Type", 1, 1, -1);
        AddCommandRule(command, 415U, "CC-Request-Number", 1, 1, -1);
        AddCommandRule(command, 1U, "User-Name", 0, 1, -1);
        AddCommandRule(command, 418U, "CC-Session-Failover", 0, 1, -1);
        AddCommandRule(command, 419U, "CC-Sub-Session-Id", 0, 1, -1);
        AddCommandRule(command, 50U, "Acct-Multi-Session-Id", 0, 1, -1);
        AddCommandRule(command, 278U, "Origin-State-Id", 0, 1, -1);
        AddCommandRule(command, 55U, "Event-Timestamp", 0, 1, -1);
        AddCommandRule(command, 431U, "Granted-Service-Unit", 0, 1, -1);
        AddCommandRule(command, 456U, "Multiple-Services-Credit-Control", 0, -1, -1);
        AddCommandRule(command, 423U, "Cost-Information", 0, 1, -1);
        AddCommandRule(command, 430U, "Final-Unit-Indication", 0, 1, -1);
        AddCommandRule(command, 669U, "QoS-Final-Unit-Indication", 0, 1, -1);
        AddCommandRule(command, 422U, "Check-Balance-Result", 0, 1, -1);
        AddCommandRule(command, 427U, "Credit-Control-Failure-Handling", 0, 1, -1);
        AddCommandRule(command, 428U, "Direct-Debiting-Failure-Handling", 0, 1, -1);
        AddCommandRule(command, 448U, "Validity-Time", 0, 1, -1);
        AddCommandRule(command, 292U, "Redirect-Host", 0, -1, -1);
        AddCommandRule(command, 261U, "Redirect-Host-Usage", 0, 1, -1);
        AddCommandRule(command, 262U, "Redirect-Max-Cache-Time", 0, 1, -1);
        AddCommandRule(command, 284U, "Proxy-Info", 0, -1, -1);
        AddCommandRule(command, 282U, "Route-Record", 0, -1, -1);
        AddCommandRule(command, 279U, "Failed-AVP", 0, -1, -1);
    }

    internal static void AddReAuthCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = Rfc6733Dictionary.CreateCommand(
            DiameterApplicationIds.CreditControl,
            DiameterCommandCodes.ReAuth,
            "Re-Auth",
            true,
            true);
        AddRfcReAuthRequestRules(request);
        AddCreditControlReAuthRules(request);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = Rfc6733Dictionary.CreateCommand(
            DiameterApplicationIds.CreditControl,
            DiameterCommandCodes.ReAuth,
            "Re-Auth",
            false,
            true);
        AddRfcReAuthAnswerRules(answer);
        AddCreditControlReAuthRules(answer);
        module.Commands.Add(answer);
    }

    internal static void AddRfcReAuthRequestRules(DiameterCommandDefinition command)
    {
        AddCommandRule(command, 263U, "Session-Id", 1, 1, 0);
        AddCommandRule(command, 264U, "Origin-Host", 1, 1, -1);
        AddCommandRule(command, 296U, "Origin-Realm", 1, 1, -1);
        AddCommandRule(command, 283U, "Destination-Realm", 1, 1, -1);
        AddCommandRule(command, 293U, "Destination-Host", 1, 1, -1);
        AddCommandRule(command, 258U, "Auth-Application-Id", 1, 1, -1);
        AddCommandRule(command, 285U, "Re-Auth-Request-Type", 1, 1, -1);
        AddCommandRule(command, 1U, "User-Name", 0, 1, -1);
        AddCommandRule(command, 278U, "Origin-State-Id", 0, 1, -1);
        AddCommandRule(command, 284U, "Proxy-Info", 0, -1, -1);
        AddCommandRule(command, 282U, "Route-Record", 0, -1, -1);
    }

    internal static void AddRfcReAuthAnswerRules(DiameterCommandDefinition command)
    {
        AddCommandRule(command, 263U, "Session-Id", 1, 1, 0);
        AddCommandRule(command, 268U, "Result-Code", 1, 1, -1);
        AddCommandRule(command, 264U, "Origin-Host", 1, 1, -1);
        AddCommandRule(command, 296U, "Origin-Realm", 1, 1, -1);
        AddCommandRule(command, 1U, "User-Name", 0, 1, -1);
        AddCommandRule(command, 278U, "Origin-State-Id", 0, 1, -1);
        AddCommandRule(command, 281U, "Error-Message", 0, 1, -1);
        AddCommandRule(command, 294U, "Error-Reporting-Host", 0, 1, -1);
        AddCommandRule(command, 279U, "Failed-AVP", 0, 1, -1);
        AddCommandRule(command, 292U, "Redirect-Host", 0, -1, -1);
        AddCommandRule(command, 261U, "Redirect-Host-Usage", 0, 1, -1);
        AddCommandRule(command, 262U, "Redirect-Max-Cache-Time", 0, 1, -1);
        AddCommandRule(command, 284U, "Proxy-Info", 0, -1, -1);
    }

    internal static void AddCreditControlReAuthRules(DiameterCommandDefinition command)
    {
        AddCommandRule(command, 419U, "CC-Sub-Session-Id", 0, 1, -1);
        AddCommandRule(command, 453U, "G-S-U-Pool-Identifier", 0, 1, -1);
        AddCommandRule(command, 439U, "Service-Identifier", 0, 1, -1);
        AddCommandRule(command, 432U, "Rating-Group", 0, 1, -1);
    }

    internal static DiameterTypeDefinition CreateEnumeratedType(string name)
    {
        return Rfc6733Dictionary.CreateType(
            name,
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4);
    }

    internal static DiameterEnumValueDefinition CreateValue(
        string name,
        int value,
        string typeName,
        uint avpCode)
    {
        return Rfc6733Dictionary.CreateEnumValue(name, value, typeName, avpCode);
    }

    internal static void AddAvp(
        DiameterDictionaryModule module,
        uint code,
        string name,
        string typeName,
        DiameterAvpBaseType baseType,
        bool mandatory)
    {
        DiameterAvpDefinition avp = new DiameterAvpDefinition();
        avp.Code = code;
        avp.VendorId = DiameterVendorIds.Ietf;
        avp.Name = name;
        avp.TypeName = typeName;
        avp.BaseType = baseType;
        if (mandatory)
        {
            avp.RequiredFlags = DiameterAvpFlags.Mandatory;
        }

        avp.ProhibitedFlags = DiameterAvpFlags.VendorSpecific;
        avp.FixedDataLength = FindFixedLength(baseType, typeName);
        module.Avps.Add(avp);
    }

    internal static DiameterAvpDefinition CreateGrouped(
        uint code,
        string name,
        bool mandatory,
        bool allowUnlisted)
    {
        DiameterAvpDefinition avp = new DiameterAvpDefinition();
        avp.Code = code;
        avp.VendorId = DiameterVendorIds.Ietf;
        avp.Name = name;
        avp.TypeName = "Grouped";
        avp.BaseType = DiameterAvpBaseType.Grouped;
        if (mandatory)
        {
            avp.RequiredFlags = DiameterAvpFlags.Mandatory;
        }

        avp.ProhibitedFlags = DiameterAvpFlags.VendorSpecific;
        avp.AllowUnlistedAvps = allowUnlisted;
        return avp;
    }

    internal static int FindFixedLength(DiameterAvpBaseType baseType, string typeName)
    {
        if (typeName == "Time" || baseType == DiameterAvpBaseType.Integer32 ||
            baseType == DiameterAvpBaseType.Unsigned32 ||
            baseType == DiameterAvpBaseType.Float32)
        {
            return 4;
        }

        if (baseType == DiameterAvpBaseType.Integer64 ||
            baseType == DiameterAvpBaseType.Unsigned64 ||
            baseType == DiameterAvpBaseType.Float64)
        {
            return 8;
        }

        return -1;
    }

    internal static void AddGroupedRule(
        DiameterAvpDefinition avp,
        uint code,
        string name,
        int minimum,
        int maximum)
    {
        avp.GroupedRules.Add(Rfc6733Dictionary.CreateRule(
            code,
            name,
            minimum,
            maximum,
            -1));
    }

    internal static void AddCommandRule(
        DiameterCommandDefinition command,
        uint code,
        string name,
        int minimum,
        int maximum,
        int fixedPosition)
    {
        command.Rules.Add(Rfc6733Dictionary.CreateRule(
            code,
            name,
            minimum,
            maximum,
            fixedPosition));
    }
}
