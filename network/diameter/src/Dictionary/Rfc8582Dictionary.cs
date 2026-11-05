namespace diameter;

internal static class Rfc8582Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc8582";
        module.Version = "RFC 8582";
        module.Dependencies.Add("rfc8581");
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "OLR_RATE_ALGORITHM",
            4,
            "",
            DiameterAvpCodes.OcFeatureVector));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.OcMaximumRate,
            "OC-Maximum-Rate",
            "Unsigned32",
            DiameterAvpBaseType.Unsigned32));
        return module;
    }
}
