namespace diameter;

internal static class Rfc7683Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc7683";
        module.Version = "RFC 7683";
        module.Dependencies.Add("rfc6733");
        AddTypes(module);
        AddValues(module);
        AddAvps(module);
        return module;
    }

    internal static void AddTypes(DiameterDictionaryModule module)
    {
        module.Types.Add(RfcExtensionDictionaryHelpers.CreateEnumeratedType("OC-Report-Type"));
    }

    internal static void AddValues(DiameterDictionaryModule module)
    {
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "OLR_DEFAULT_ALGO",
            1,
            "",
            DiameterAvpCodes.OcFeatureVector));
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "HOST_REPORT",
            0,
            "OC-Report-Type",
            DiameterAvpCodes.OcReportType));
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "REALM_REPORT",
            1,
            "OC-Report-Type",
            DiameterAvpCodes.OcReportType));
    }

    internal static void AddAvps(DiameterDictionaryModule module)
    {
        DiameterAvpDefinition supported = RfcExtensionDictionaryHelpers.CreateGrouped(
            DiameterAvpCodes.OcSupportedFeatures,
            "OC-Supported-Features");
        RfcExtensionDictionaryHelpers.AddGroupedRule(
            supported,
            DiameterAvpCodes.OcFeatureVector,
            "OC-Feature-Vector",
            0,
            1);
        module.Avps.Add(supported);

        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.OcFeatureVector,
            "OC-Feature-Vector",
            "Unsigned64",
            DiameterAvpBaseType.Unsigned64));

        DiameterAvpDefinition report = RfcExtensionDictionaryHelpers.CreateGrouped(
            DiameterAvpCodes.OcOlr,
            "OC-OLR");
        RfcExtensionDictionaryHelpers.AddGroupedRule(report, DiameterAvpCodes.OcSequenceNumber, "OC-Sequence-Number", 1, 1);
        RfcExtensionDictionaryHelpers.AddGroupedRule(report, DiameterAvpCodes.OcReportType, "OC-Report-Type", 1, 1);
        RfcExtensionDictionaryHelpers.AddGroupedRule(report, DiameterAvpCodes.OcReductionPercentage, "OC-Reduction-Percentage", 0, 1);
        RfcExtensionDictionaryHelpers.AddGroupedRule(report, DiameterAvpCodes.OcValidityDuration, "OC-Validity-Duration", 0, 1);
        module.Avps.Add(report);

        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.OcSequenceNumber,
            "OC-Sequence-Number",
            "Unsigned64",
            DiameterAvpBaseType.Unsigned64));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.OcValidityDuration,
            "OC-Validity-Duration",
            "Unsigned32",
            DiameterAvpBaseType.Unsigned32));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.OcReportType,
            "OC-Report-Type",
            "OC-Report-Type",
            DiameterAvpBaseType.Integer32));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.OcReductionPercentage,
            "OC-Reduction-Percentage",
            "Unsigned32",
            DiameterAvpBaseType.Unsigned32));
    }
}
