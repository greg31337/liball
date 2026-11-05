namespace diameter;

internal static class Rfc7944Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc7944";
        module.Version = "RFC 7944";
        module.Dependencies.Add("rfc6733");
        module.Types.Add(RfcExtensionDictionaryHelpers.CreateEnumeratedType("DRMP"));
        AddValues(module);
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.Drmp,
            "DRMP",
            "DRMP",
            DiameterAvpBaseType.Integer32));
        return module;
    }

    internal static void AddValues(DiameterDictionaryModule module)
    {
        int priority = 0;
        while (priority <= 15)
        {
            module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
                "PRIORITY_" + priority,
                priority,
                "DRMP",
                DiameterAvpCodes.Drmp));
            priority = priority + 1;
        }
    }
}
