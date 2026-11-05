namespace diameter;

internal static class Rfc7075Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc7075";
        module.Version = "RFC 7075";
        module.Dependencies.Add("rfc6733");
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "DIAMETER_REALM_REDIRECT_INDICATION",
            (int)DiameterResultCodes.RealmRedirectIndication,
            "",
            DiameterAvpCodes.ResultCode));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.RedirectRealm,
            "Redirect-Realm",
            "DiameterIdentity",
            DiameterAvpBaseType.OctetString));
        return module;
    }
}
