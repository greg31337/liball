using System.Collections.Generic;

namespace diameter;

internal static class Rfc6733Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc6733";
        module.Version = "RFC 6733";
        AddVendorsAndApplications(module);
        AddTypes(module);
        AddEnumValues(module);
        AddAvps(module);
        AddCommands(module);
        return module;
    }

    internal static void AddVendorsAndApplications(DiameterDictionaryModule module)
    {
        DiameterVendorDefinition ietf = new DiameterVendorDefinition();
        ietf.Id = DiameterVendorIds.Ietf;
        ietf.Name = "IETF";
        module.Vendors.Add(ietf);

        DiameterApplicationDefinition common = new DiameterApplicationDefinition();
        common.Id = DiameterApplicationIds.Common;
        common.VendorId = DiameterVendorIds.Ietf;
        common.Name = "Diameter Common Messages";
        module.Applications.Add(common);

        DiameterApplicationDefinition accounting = new DiameterApplicationDefinition();
        accounting.Id = DiameterApplicationIds.BaseAccounting;
        accounting.VendorId = DiameterVendorIds.Ietf;
        accounting.Name = "Diameter Base Accounting";
        accounting.IsAccounting = true;
        module.Applications.Add(accounting);

        DiameterApplicationDefinition relay = new DiameterApplicationDefinition();
        relay.Id = DiameterApplicationIds.Relay;
        relay.VendorId = DiameterVendorIds.Ietf;
        relay.Name = "Relay";
        module.Applications.Add(relay);
    }

    internal static void AddTypes(DiameterDictionaryModule module)
    {
        module.Types.Add(CreateType("OctetString", DiameterAvpBaseType.OctetString, "", -1));
        module.Types.Add(CreateType("Integer32", DiameterAvpBaseType.Integer32, "", 4));
        module.Types.Add(CreateType("Integer64", DiameterAvpBaseType.Integer64, "", 8));
        module.Types.Add(CreateType("Unsigned32", DiameterAvpBaseType.Unsigned32, "", 4));
        module.Types.Add(CreateType("Unsigned64", DiameterAvpBaseType.Unsigned64, "", 8));
        module.Types.Add(CreateType("Float32", DiameterAvpBaseType.Float32, "", 4));
        module.Types.Add(CreateType("Float64", DiameterAvpBaseType.Float64, "", 8));
        module.Types.Add(CreateType("Grouped", DiameterAvpBaseType.Grouped, "", -1));
        module.Types.Add(CreateType(
            "Address",
            DiameterAvpBaseType.OctetString,
            "OctetString",
            -1));
        module.Types.Add(CreateType(
            "Time",
            DiameterAvpBaseType.OctetString,
            "OctetString",
            4));
        module.Types.Add(CreateType(
            "UTF8String",
            DiameterAvpBaseType.OctetString,
            "OctetString",
            -1));
        module.Types.Add(CreateType(
            "DiameterIdentity",
            DiameterAvpBaseType.OctetString,
            "OctetString",
            -1));
        module.Types.Add(CreateType(
            "DiameterURI",
            DiameterAvpBaseType.OctetString,
            "OctetString",
            -1));
        module.Types.Add(CreateType(
            "Enumerated",
            DiameterAvpBaseType.Integer32,
            "Integer32",
            4));
        module.Types.Add(CreateType(
            "IPFilterRule",
            DiameterAvpBaseType.OctetString,
            "OctetString",
            -1));
        AddEnumeratedTypes(module);
    }

    internal static void AddEnumeratedTypes(DiameterDictionaryModule module)
    {
        module.Types.Add(CreateType(
            "Disconnect-Cause",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Redirect-Host-Usage",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Auth-Request-Type",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Auth-Session-State",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Re-Auth-Request-Type",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Termination-Cause",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Session-Server-Failover",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Accounting-Record-Type",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
        module.Types.Add(CreateType(
            "Accounting-Realtime-Required",
            DiameterAvpBaseType.Integer32,
            "Enumerated",
            4));
    }

    internal static void AddEnumValues(DiameterDictionaryModule module)
    {
        AddEnumDomain(
            module,
            "Disconnect-Cause",
            DiameterAvpCodes.DisconnectCause,
            new string[] { "REBOOTING", "BUSY", "DO_NOT_WANT_TO_TALK_TO_YOU" },
            new int[] { 0, 1, 2 });
        AddEnumDomain(
            module,
            "Redirect-Host-Usage",
            DiameterAvpCodes.RedirectHostUsage,
            new string[]
            {
                "DONT_CACHE", "ALL_SESSION", "ALL_REALM", "REALM_AND_APPLICATION",
                "ALL_APPLICATION", "ALL_HOST", "ALL_USER"
            },
            new int[] { 0, 1, 2, 3, 4, 5, 6 });
        AddEnumDomain(
            module,
            "Auth-Request-Type",
            DiameterAvpCodes.AuthRequestType,
            new string[]
            {
                "AUTHENTICATE_ONLY", "AUTHORIZE_ONLY", "AUTHORIZE_AUTHENTICATE"
            },
            new int[] { 1, 2, 3 });
        AddEnumDomain(
            module,
            "Auth-Session-State",
            DiameterAvpCodes.AuthSessionState,
            new string[] { "STATE_MAINTAINED", "NO_STATE_MAINTAINED" },
            new int[] { 0, 1 });
        AddEnumDomain(
            module,
            "Re-Auth-Request-Type",
            DiameterAvpCodes.ReAuthRequestType,
            new string[] { "AUTHORIZE_ONLY", "AUTHORIZE_AUTHENTICATE" },
            new int[] { 0, 1 });
        AddEnumDomain(
            module,
            "Termination-Cause",
            DiameterAvpCodes.TerminationCause,
            new string[]
            {
                "DIAMETER_LOGOUT", "DIAMETER_SERVICE_NOT_PROVIDED", "DIAMETER_BAD_ANSWER",
                "DIAMETER_ADMINISTRATIVE", "DIAMETER_LINK_BROKEN", "DIAMETER_AUTH_EXPIRED",
                "DIAMETER_USER_MOVED", "DIAMETER_SESSION_TIMEOUT"
            },
            new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        AddEnumDomain(
            module,
            "Session-Server-Failover",
            DiameterAvpCodes.SessionServerFailover,
            new string[]
            {
                "REFUSE_SERVICE", "TRY_AGAIN", "ALLOW_SERVICE", "TRY_AGAIN_ALLOW_SERVICE"
            },
            new int[] { 0, 1, 2, 3 });
        AddEnumDomain(
            module,
            "Accounting-Record-Type",
            DiameterAvpCodes.AccountingRecordType,
            new string[] { "EVENT_RECORD", "START_RECORD", "INTERIM_RECORD", "STOP_RECORD" },
            new int[] { 1, 2, 3, 4 });
        AddEnumDomain(
            module,
            "Accounting-Realtime-Required",
            DiameterAvpCodes.AccountingRealtimeRequired,
            new string[] { "DELIVER_AND_GRANT", "GRANT_AND_STORE", "GRANT_AND_LOSE" },
            new int[] { 1, 2, 3 });

        module.EnumValues.Add(CreateEnumValue(
            "NO_INBAND_SECURITY",
            0,
            "",
            DiameterAvpCodes.InbandSecurityId));
        module.EnumValues.Add(CreateEnumValue(
            "TLS",
            1,
            "",
            DiameterAvpCodes.InbandSecurityId));
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
            module.EnumValues.Add(CreateEnumValue(
                names[index],
                values[index],
                typeName,
                avpCode));
            index = index + 1;
        }
    }

    internal static void AddAvps(DiameterDictionaryModule module)
    {
        DiameterAvpFlags mandatory = DiameterAvpFlags.Mandatory;
        DiameterAvpFlags vendorProhibited = DiameterAvpFlags.VendorSpecific;
        DiameterAvpFlags allProhibited =
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory;

        module.Avps.Add(CreateAvp(1U, "User-Name", "UTF8String", mandatory, vendorProhibited));
        module.Avps.Add(CreateAvp(25U, "Class", "OctetString", mandatory, vendorProhibited));
        module.Avps.Add(CreateAvp(
            27U,
            "Session-Timeout",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ProxyState,
            "Proxy-State",
            "OctetString",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AcctSessionId,
            "Acct-Session-Id",
            "OctetString",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AcctMultiSessionId,
            "Acct-Multi-Session-Id",
            "UTF8String",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.EventTimestamp,
            "Event-Timestamp",
            "Time",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AcctInterimInterval,
            "Acct-Interim-Interval",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.HostIpAddress,
            "Host-IP-Address",
            "Address",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AuthApplicationId,
            "Auth-Application-Id",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AcctApplicationId,
            "Acct-Application-Id",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        AddVendorSpecificApplicationId(module, mandatory, vendorProhibited);
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.RedirectHostUsage,
            "Redirect-Host-Usage",
            "Redirect-Host-Usage",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.RedirectMaxCacheTime,
            "Redirect-Max-Cache-Time",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.SessionId,
            "Session-Id",
            "UTF8String",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginHost,
            "Origin-Host",
            "DiameterIdentity",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.SupportedVendorId,
            "Supported-Vendor-Id",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.VendorId,
            "Vendor-Id",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.FirmwareRevision,
            "Firmware-Revision",
            "Unsigned32",
            DiameterAvpFlags.None,
            allProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ResultCode,
            "Result-Code",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ProductName,
            "Product-Name",
            "UTF8String",
            DiameterAvpFlags.None,
            allProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.SessionBinding,
            "Session-Binding",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.SessionServerFailover,
            "Session-Server-Failover",
            "Session-Server-Failover",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.MultiRoundTimeOut,
            "Multi-Round-Time-Out",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.DisconnectCause,
            "Disconnect-Cause",
            "Disconnect-Cause",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AuthRequestType,
            "Auth-Request-Type",
            "Auth-Request-Type",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AuthGracePeriod,
            "Auth-Grace-Period",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AuthSessionState,
            "Auth-Session-State",
            "Auth-Session-State",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginStateId,
            "Origin-State-Id",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        AddFailedAvp(module, mandatory, vendorProhibited);
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ProxyHost,
            "Proxy-Host",
            "DiameterIdentity",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ErrorMessage,
            "Error-Message",
            "UTF8String",
            DiameterAvpFlags.None,
            allProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.RouteRecord,
            "Route-Record",
            "DiameterIdentity",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.DestinationRealm,
            "Destination-Realm",
            "DiameterIdentity",
            mandatory,
            vendorProhibited));
        AddProxyInfo(module, mandatory, vendorProhibited);
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ReAuthRequestType,
            "Re-Auth-Request-Type",
            "Re-Auth-Request-Type",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AccountingSubSessionId,
            "Accounting-Sub-Session-Id",
            "Unsigned64",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AuthorizationLifetime,
            "Authorization-Lifetime",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.RedirectHost,
            "Redirect-Host",
            "DiameterURI",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.DestinationHost,
            "Destination-Host",
            "DiameterIdentity",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ErrorReportingHost,
            "Error-Reporting-Host",
            "DiameterIdentity",
            DiameterAvpFlags.None,
            allProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.TerminationCause,
            "Termination-Cause",
            "Termination-Cause",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.OriginRealm,
            "Origin-Realm",
            "DiameterIdentity",
            mandatory,
            vendorProhibited));
        AddExperimentalResult(module, mandatory, vendorProhibited);
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.ExperimentalResultCode,
            "Experimental-Result-Code",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.InbandSecurityId,
            "Inband-Security-Id",
            "Unsigned32",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AccountingRecordType,
            "Accounting-Record-Type",
            "Accounting-Record-Type",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AccountingRealtimeRequired,
            "Accounting-Realtime-Required",
            "Accounting-Realtime-Required",
            mandatory,
            vendorProhibited));
        module.Avps.Add(CreateAvp(
            DiameterAvpCodes.AccountingRecordNumber,
            "Accounting-Record-Number",
            "Unsigned32",
            mandatory,
            vendorProhibited));
    }

    internal static void AddVendorSpecificApplicationId(
        DiameterDictionaryModule module,
        DiameterAvpFlags required,
        DiameterAvpFlags prohibited)
    {
        DiameterAvpDefinition avp = CreateAvp(
            DiameterAvpCodes.VendorSpecificApplicationId,
            "Vendor-Specific-Application-Id",
            "Grouped",
            required,
            prohibited);
        avp.AllowUnlistedAvps = false;
        avp.GroupedRules.Add(CreateRule(DiameterAvpCodes.VendorId, "Vendor-Id", 1, 1, -1));
        avp.GroupedRules.Add(CreateRule(
            DiameterAvpCodes.AuthApplicationId,
            "Auth-Application-Id",
            0,
            1,
            -1));
        avp.GroupedRules.Add(CreateRule(
            DiameterAvpCodes.AcctApplicationId,
            "Acct-Application-Id",
            0,
            1,
            -1));
        module.Avps.Add(avp);
    }

    internal static void AddFailedAvp(
        DiameterDictionaryModule module,
        DiameterAvpFlags required,
        DiameterAvpFlags prohibited)
    {
        DiameterAvpDefinition avp = CreateAvp(
            DiameterAvpCodes.FailedAvp,
            "Failed-AVP",
            "Grouped",
            required,
            prohibited);
        avp.AllowUnlistedAvps = true;
        avp.GroupedRules.Add(CreateRule(0U, "AVP", 1, -1, -1));
        module.Avps.Add(avp);
    }

    internal static void AddProxyInfo(
        DiameterDictionaryModule module,
        DiameterAvpFlags required,
        DiameterAvpFlags prohibited)
    {
        DiameterAvpDefinition avp = CreateAvp(
            DiameterAvpCodes.ProxyInfo,
            "Proxy-Info",
            "Grouped",
            required,
            prohibited);
        avp.AllowUnlistedAvps = true;
        avp.GroupedRules.Add(CreateRule(DiameterAvpCodes.ProxyHost, "Proxy-Host", 1, 1, -1));
        avp.GroupedRules.Add(CreateRule(
            DiameterAvpCodes.ProxyState,
            "Proxy-State",
            1,
            1,
            -1));
        module.Avps.Add(avp);
    }

    internal static void AddExperimentalResult(
        DiameterDictionaryModule module,
        DiameterAvpFlags required,
        DiameterAvpFlags prohibited)
    {
        DiameterAvpDefinition avp = CreateAvp(
            DiameterAvpCodes.ExperimentalResult,
            "Experimental-Result",
            "Grouped",
            required,
            prohibited);
        avp.AllowUnlistedAvps = false;
        avp.GroupedRules.Add(CreateRule(DiameterAvpCodes.VendorId, "Vendor-Id", 1, 1, -1));
        avp.GroupedRules.Add(CreateRule(
            DiameterAvpCodes.ExperimentalResultCode,
            "Experimental-Result-Code",
            1,
            1,
            -1));
        module.Avps.Add(avp);
    }

    internal static void AddCommands(DiameterDictionaryModule module)
    {
        AddCapabilitiesCommands(module);
        AddPeerCommands(module);
        AddSessionCommands(module);
        AddAccountingCommands(module);
    }

    internal static void AddCapabilitiesCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.CapabilitiesExchange,
            "Capabilities-Exchange",
            true,
            false);
        AddRule(request, DiameterAvpCodes.OriginHost, "Origin-Host", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.OriginRealm, "Origin-Realm", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.HostIpAddress, "Host-IP-Address", 1, -1, -1);
        AddRule(request, DiameterAvpCodes.VendorId, "Vendor-Id", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.ProductName, "Product-Name", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        AddRule(request, DiameterAvpCodes.SupportedVendorId, "Supported-Vendor-Id", 0, -1, -1);
        AddRule(request, DiameterAvpCodes.AuthApplicationId, "Auth-Application-Id", 0, -1, -1);
        AddRule(request, DiameterAvpCodes.InbandSecurityId, "Inband-Security-Id", 0, -1, -1);
        AddRule(request, DiameterAvpCodes.AcctApplicationId, "Acct-Application-Id", 0, -1, -1);
        AddRule(
            request,
            DiameterAvpCodes.VendorSpecificApplicationId,
            "Vendor-Specific-Application-Id",
            0,
            -1,
            -1);
        AddRule(request, DiameterAvpCodes.FirmwareRevision, "Firmware-Revision", 0, 1, -1);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.CapabilitiesExchange,
            "Capabilities-Exchange",
            false,
            false);
        AddRule(answer, DiameterAvpCodes.ResultCode, "Result-Code", 1, 1, -1);
        CopyCapabilitiesRules(request, answer);
        AddRule(answer, DiameterAvpCodes.ErrorMessage, "Error-Message", 0, 1, -1);
        AddRule(answer, DiameterAvpCodes.FailedAvp, "Failed-AVP", 0, 1, -1);
        module.Commands.Add(answer);
    }

    internal static void CopyCapabilitiesRules(
        DiameterCommandDefinition request,
        DiameterCommandDefinition answer)
    {
        int index = 0;
        while (index < request.Rules.Count)
        {
            DiameterAvpRuleDefinition rule = request.Rules[index];
            answer.Rules.Add(CreateRule(
                rule.AvpCode,
                rule.AvpName,
                rule.MinimumCount,
                rule.MaximumCount,
                rule.FixedHeadPosition));
            index = index + 1;
        }
    }

    internal static void AddPeerCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition dpr = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DisconnectPeer,
            "Disconnect-Peer",
            true,
            false);
        AddOriginRules(dpr);
        AddRule(dpr, DiameterAvpCodes.DisconnectCause, "Disconnect-Cause", 1, 1, -1);
        module.Commands.Add(dpr);

        DiameterCommandDefinition dpa = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DisconnectPeer,
            "Disconnect-Peer",
            false,
            false);
        AddRule(dpa, DiameterAvpCodes.ResultCode, "Result-Code", 1, 1, -1);
        AddOriginRules(dpa);
        AddAnswerErrorRules(dpa, false);
        module.Commands.Add(dpa);

        DiameterCommandDefinition dwr = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DeviceWatchdog,
            "Device-Watchdog",
            true,
            false);
        AddOriginRules(dwr);
        AddRule(dwr, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        module.Commands.Add(dwr);

        DiameterCommandDefinition dwa = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.DeviceWatchdog,
            "Device-Watchdog",
            false,
            false);
        AddRule(dwa, DiameterAvpCodes.ResultCode, "Result-Code", 1, 1, -1);
        AddOriginRules(dwa);
        AddAnswerErrorRules(dwa, false);
        AddRule(dwa, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        module.Commands.Add(dwa);
    }

    internal static void AddSessionCommands(DiameterDictionaryModule module)
    {
        AddReAuthCommands(module);
        AddSessionTerminationCommands(module);
        AddAbortSessionCommands(module);
    }

    internal static void AddReAuthCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.ReAuth,
            "Re-Auth",
            true,
            true);
        AddRule(request, DiameterAvpCodes.SessionId, "Session-Id", 1, 1, 0);
        AddOriginRules(request);
        AddRule(request, DiameterAvpCodes.DestinationRealm, "Destination-Realm", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.DestinationHost, "Destination-Host", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.AuthApplicationId, "Auth-Application-Id", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.ReAuthRequestType, "Re-Auth-Request-Type", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.UserName, "User-Name", 0, 1, -1);
        AddRule(request, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        AddProxyAndRouteRules(request, true);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.ReAuth,
            "Re-Auth",
            false,
            true);
        AddSessionAnswerRules(answer, false);
        module.Commands.Add(answer);
    }

    internal static void AddSessionTerminationCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.SessionTermination,
            "Session-Termination",
            true,
            true);
        AddRule(request, DiameterAvpCodes.SessionId, "Session-Id", 1, 1, 0);
        AddOriginRules(request);
        AddRule(request, DiameterAvpCodes.DestinationRealm, "Destination-Realm", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.AuthApplicationId, "Auth-Application-Id", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.TerminationCause, "Termination-Cause", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.UserName, "User-Name", 0, 1, -1);
        AddRule(request, DiameterAvpCodes.DestinationHost, "Destination-Host", 0, 1, -1);
        AddRule(request, DiameterAvpCodes.Class, "Class", 0, -1, -1);
        AddRule(request, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        AddProxyAndRouteRules(request, true);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.SessionTermination,
            "Session-Termination",
            false,
            true);
        AddSessionAnswerRules(answer, true);
        module.Commands.Add(answer);
    }

    internal static void AddAbortSessionCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.AbortSession,
            "Abort-Session",
            true,
            true);
        AddRule(request, DiameterAvpCodes.SessionId, "Session-Id", 1, 1, 0);
        AddOriginRules(request);
        AddRule(request, DiameterAvpCodes.DestinationRealm, "Destination-Realm", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.DestinationHost, "Destination-Host", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.AuthApplicationId, "Auth-Application-Id", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.UserName, "User-Name", 0, 1, -1);
        AddRule(request, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        AddProxyAndRouteRules(request, true);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = CreateCommand(
            DiameterApplicationIds.Common,
            DiameterCommandCodes.AbortSession,
            "Abort-Session",
            false,
            true);
        AddSessionAnswerRules(answer, false);
        module.Commands.Add(answer);
    }

    internal static void AddSessionAnswerRules(
        DiameterCommandDefinition command,
        bool includeClass)
    {
        AddRule(command, DiameterAvpCodes.SessionId, "Session-Id", 1, 1, 0);
        AddRule(command, DiameterAvpCodes.ResultCode, "Result-Code", 1, 1, -1);
        AddOriginRules(command);
        AddRule(command, DiameterAvpCodes.UserName, "User-Name", 0, 1, -1);
        if (includeClass)
        {
            AddRule(command, DiameterAvpCodes.Class, "Class", 0, -1, -1);
        }

        AddRule(command, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        AddAnswerErrorRules(command, true);
        AddRedirectRules(command);
        AddRule(command, DiameterAvpCodes.ProxyInfo, "Proxy-Info", 0, -1, -1);
    }

    internal static void AddAccountingCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = CreateCommand(
            DiameterApplicationIds.BaseAccounting,
            DiameterCommandCodes.Accounting,
            "Accounting",
            true,
            true);
        AddRule(request, DiameterAvpCodes.SessionId, "Session-Id", 1, 1, 0);
        AddOriginRules(request);
        AddRule(request, DiameterAvpCodes.DestinationRealm, "Destination-Realm", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.AccountingRecordType, "Accounting-Record-Type", 1, 1, -1);
        AddRule(request, DiameterAvpCodes.AccountingRecordNumber, "Accounting-Record-Number", 1, 1, -1);
        AddAccountingOptionalRules(request, true);
        AddRule(request, DiameterAvpCodes.ProxyInfo, "Proxy-Info", 0, -1, -1);
        AddRule(request, DiameterAvpCodes.RouteRecord, "Route-Record", 0, -1, -1);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = CreateCommand(
            DiameterApplicationIds.BaseAccounting,
            DiameterCommandCodes.Accounting,
            "Accounting",
            false,
            true);
        AddRule(answer, DiameterAvpCodes.SessionId, "Session-Id", 1, 1, 0);
        AddRule(answer, DiameterAvpCodes.ResultCode, "Result-Code", 1, 1, -1);
        AddOriginRules(answer);
        AddRule(answer, DiameterAvpCodes.AccountingRecordType, "Accounting-Record-Type", 1, 1, -1);
        AddRule(answer, DiameterAvpCodes.AccountingRecordNumber, "Accounting-Record-Number", 1, 1, -1);
        AddAccountingOptionalRules(answer, false);
        AddAnswerErrorRules(answer, true);
        AddRule(answer, DiameterAvpCodes.ProxyInfo, "Proxy-Info", 0, -1, -1);
        module.Commands.Add(answer);
    }

    internal static void AddAccountingOptionalRules(
        DiameterCommandDefinition command,
        bool includeDestinationHost)
    {
        AddRule(command, DiameterAvpCodes.AcctApplicationId, "Acct-Application-Id", 0, 1, -1);
        AddRule(
            command,
            DiameterAvpCodes.VendorSpecificApplicationId,
            "Vendor-Specific-Application-Id",
            0,
            1,
            -1);
        AddRule(command, DiameterAvpCodes.UserName, "User-Name", 0, 1, -1);
        if (includeDestinationHost)
        {
            AddRule(command, DiameterAvpCodes.DestinationHost, "Destination-Host", 0, 1, -1);
        }

        AddRule(
            command,
            DiameterAvpCodes.AccountingSubSessionId,
            "Accounting-Sub-Session-Id",
            0,
            1,
            -1);
        AddRule(command, DiameterAvpCodes.AcctSessionId, "Acct-Session-Id", 0, 1, -1);
        AddRule(command, DiameterAvpCodes.AcctMultiSessionId, "Acct-Multi-Session-Id", 0, 1, -1);
        AddRule(command, DiameterAvpCodes.AcctInterimInterval, "Acct-Interim-Interval", 0, 1, -1);
        AddRule(
            command,
            DiameterAvpCodes.AccountingRealtimeRequired,
            "Accounting-Realtime-Required",
            0,
            1,
            -1);
        AddRule(command, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        AddRule(command, DiameterAvpCodes.EventTimestamp, "Event-Timestamp", 0, 1, -1);
    }

    internal static void AddOriginRules(DiameterCommandDefinition command)
    {
        AddRule(command, DiameterAvpCodes.OriginHost, "Origin-Host", 1, 1, -1);
        AddRule(command, DiameterAvpCodes.OriginRealm, "Origin-Realm", 1, 1, -1);
    }

    internal static void AddProxyAndRouteRules(
        DiameterCommandDefinition command,
        bool includeRouteRecord)
    {
        AddRule(command, DiameterAvpCodes.ProxyInfo, "Proxy-Info", 0, -1, -1);
        if (includeRouteRecord)
        {
            AddRule(command, DiameterAvpCodes.RouteRecord, "Route-Record", 0, -1, -1);
        }
    }

    internal static void AddAnswerErrorRules(
        DiameterCommandDefinition command,
        bool includeReportingHost)
    {
        AddRule(command, DiameterAvpCodes.ErrorMessage, "Error-Message", 0, 1, -1);
        if (includeReportingHost)
        {
            AddRule(
                command,
                DiameterAvpCodes.ErrorReportingHost,
                "Error-Reporting-Host",
                0,
                1,
                -1);
        }

        AddRule(command, DiameterAvpCodes.FailedAvp, "Failed-AVP", 0, 1, -1);
    }

    internal static void AddRedirectRules(DiameterCommandDefinition command)
    {
        AddRule(command, DiameterAvpCodes.RedirectHost, "Redirect-Host", 0, -1, -1);
        AddRule(command, DiameterAvpCodes.RedirectHostUsage, "Redirect-Host-Usage", 0, 1, -1);
        AddRule(
            command,
            DiameterAvpCodes.RedirectMaxCacheTime,
            "Redirect-Max-Cache-Time",
            0,
            1,
            -1);
    }

    internal static DiameterTypeDefinition CreateType(
        string name,
        DiameterAvpBaseType baseType,
        string parentName,
        int fixedLength)
    {
        DiameterTypeDefinition result = new DiameterTypeDefinition();
        result.Name = name;
        result.VendorId = DiameterVendorIds.Ietf;
        result.BaseType = baseType;
        result.ParentTypeName = parentName;
        result.ParentTypeVendorId = DiameterVendorIds.Ietf;
        result.FixedDataLength = fixedLength;
        return result;
    }

    internal static DiameterEnumValueDefinition CreateEnumValue(
        string name,
        int value,
        string typeName,
        uint avpCode)
    {
        DiameterEnumValueDefinition result = new DiameterEnumValueDefinition();
        result.Name = name;
        result.Value = value;
        result.TypeName = typeName;
        result.TypeVendorId = DiameterVendorIds.Ietf;
        result.AvpCode = avpCode;
        result.AvpVendorId = DiameterVendorIds.Ietf;
        return result;
    }

    internal static DiameterAvpDefinition CreateAvp(
        uint code,
        string name,
        string typeName,
        DiameterAvpFlags required,
        DiameterAvpFlags prohibited)
    {
        DiameterAvpDefinition result = new DiameterAvpDefinition();
        result.Code = code;
        result.VendorId = DiameterVendorIds.Ietf;
        result.Name = name;
        result.TypeName = typeName;
        result.BaseType = FindBaseType(typeName);
        result.RequiredFlags = required;
        result.ProhibitedFlags = prohibited;
        result.FixedDataLength = FindFixedLength(result.BaseType, typeName);
        return result;
    }

    internal static DiameterAvpBaseType FindBaseType(string typeName)
    {
        if (typeName == "Integer32" || typeName == "Enumerated" ||
            typeName == "Disconnect-Cause" || typeName == "Redirect-Host-Usage" ||
            typeName == "Auth-Request-Type" || typeName == "Auth-Session-State" ||
            typeName == "Re-Auth-Request-Type" || typeName == "Termination-Cause" ||
            typeName == "Session-Server-Failover" ||
            typeName == "Accounting-Record-Type" ||
            typeName == "Accounting-Realtime-Required")
        {
            return DiameterAvpBaseType.Integer32;
        }

        if (typeName == "Integer64")
        {
            return DiameterAvpBaseType.Integer64;
        }

        if (typeName == "Unsigned32")
        {
            return DiameterAvpBaseType.Unsigned32;
        }

        if (typeName == "Unsigned64")
        {
            return DiameterAvpBaseType.Unsigned64;
        }

        if (typeName == "Float32")
        {
            return DiameterAvpBaseType.Float32;
        }

        if (typeName == "Float64")
        {
            return DiameterAvpBaseType.Float64;
        }

        if (typeName == "Grouped")
        {
            return DiameterAvpBaseType.Grouped;
        }

        return DiameterAvpBaseType.OctetString;
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

    internal static DiameterCommandDefinition CreateCommand(
        uint applicationId,
        uint code,
        string name,
        bool request,
        bool proxyable)
    {
        DiameterCommandDefinition result = new DiameterCommandDefinition();
        result.ApplicationId = applicationId;
        result.Code = code;
        result.Name = name;
        result.IsRequest = request;
        if (request)
        {
            result.RequiredFlags = DiameterMessageFlags.Request;
            result.ProhibitedFlags = DiameterMessageFlags.Error;
            if (proxyable)
            {
                result.RequiredFlags = result.RequiredFlags | DiameterMessageFlags.Proxyable;
            }
            else
            {
                result.ProhibitedFlags = result.ProhibitedFlags |
                    DiameterMessageFlags.Proxyable |
                    DiameterMessageFlags.PotentiallyRetransmitted;
            }
        }
        else
        {
            result.ProhibitedFlags = DiameterMessageFlags.Request |
                DiameterMessageFlags.Error |
                DiameterMessageFlags.PotentiallyRetransmitted;
            if (proxyable)
            {
                result.RequiredFlags = DiameterMessageFlags.Proxyable;
            }
            else
            {
                result.ProhibitedFlags = result.ProhibitedFlags |
                    DiameterMessageFlags.Proxyable;
            }
        }

        return result;
    }

    internal static void AddRule(
        DiameterCommandDefinition command,
        uint avpCode,
        string avpName,
        int minimum,
        int maximum,
        int fixedPosition)
    {
        command.Rules.Add(CreateRule(
            avpCode,
            avpName,
            minimum,
            maximum,
            fixedPosition));
    }

    internal static DiameterAvpRuleDefinition CreateRule(
        uint avpCode,
        string avpName,
        int minimum,
        int maximum,
        int fixedPosition)
    {
        DiameterAvpRuleDefinition result = new DiameterAvpRuleDefinition();
        result.AvpCode = avpCode;
        result.AvpVendorId = DiameterVendorIds.Ietf;
        result.AvpName = avpName;
        result.MinimumCount = minimum;
        result.MaximumCount = maximum;
        result.FixedHeadPosition = fixedPosition;
        return result;
    }
}
