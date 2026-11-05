namespace diameter;

internal static class Rfc6737Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc6737";
        module.Version = "RFC 6737";
        module.Dependencies.Add("rfc6733");
        AddApplication(module);
        AddCommands(module);
        return module;
    }

    internal static void AddApplication(DiameterDictionaryModule module)
    {
        DiameterApplicationDefinition application = new DiameterApplicationDefinition();
        application.Id = DiameterApplicationIds.CapabilitiesUpdate;
        application.VendorId = DiameterVendorIds.Ietf;
        application.Name = "Diameter Capabilities Update";
        module.Applications.Add(application);
    }

    internal static void AddCommands(DiameterDictionaryModule module)
    {
        DiameterCommandDefinition request = Rfc6733Dictionary.CreateCommand(
            DiameterApplicationIds.CapabilitiesUpdate,
            DiameterCommandCodes.CapabilitiesUpdate,
            "Capabilities-Update",
            true,
            false);
        AddRequestRules(request);
        module.Commands.Add(request);

        DiameterCommandDefinition answer = Rfc6733Dictionary.CreateCommand(
            DiameterApplicationIds.CapabilitiesUpdate,
            DiameterCommandCodes.CapabilitiesUpdate,
            "Capabilities-Update",
            false,
            false);
        Rfc6733Dictionary.AddOriginRules(answer);
        Rfc6733Dictionary.AddRule(answer, DiameterAvpCodes.ResultCode, "Result-Code", 1, 1, -1);
        Rfc6733Dictionary.AddRule(answer, DiameterAvpCodes.ErrorMessage, "Error-Message", 0, 1, -1);
        module.Commands.Add(answer);
    }

    internal static void AddRequestRules(DiameterCommandDefinition request)
    {
        Rfc6733Dictionary.AddOriginRules(request);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.HostIpAddress, "Host-IP-Address", 1, -1, -1);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.VendorId, "Vendor-Id", 1, 1, -1);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.ProductName, "Product-Name", 1, 1, -1);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.OriginStateId, "Origin-State-Id", 0, 1, -1);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.SupportedVendorId, "Supported-Vendor-Id", 0, -1, -1);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.AuthApplicationId, "Auth-Application-Id", 0, -1, -1);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.AcctApplicationId, "Acct-Application-Id", 0, -1, -1);
        Rfc6733Dictionary.AddRule(
            request,
            DiameterAvpCodes.VendorSpecificApplicationId,
            "Vendor-Specific-Application-Id",
            0,
            -1,
            -1);
        Rfc6733Dictionary.AddRule(request, DiameterAvpCodes.FirmwareRevision, "Firmware-Revision", 0, 1, -1);
    }
}
