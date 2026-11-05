namespace diameter;

internal static class Rfc8581Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc8581";
        module.Version = "RFC 8581";
        module.Dependencies.Add("rfc7683");
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "OC_PEER_REPORT",
            16,
            "",
            DiameterAvpCodes.OcFeatureVector));
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "PEER_REPORT",
            2,
            "OC-Report-Type",
            DiameterAvpCodes.OcReportType));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.OcPeerAlgorithm,
            "OC-Peer-Algo",
            "Unsigned64",
            DiameterAvpBaseType.Unsigned64));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.SourceId,
            "SourceID",
            "DiameterIdentity",
            DiameterAvpBaseType.OctetString));
        return module;
    }
}
