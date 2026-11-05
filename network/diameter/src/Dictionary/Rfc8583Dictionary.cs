namespace diameter;

internal static class Rfc8583Dictionary
{
    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc8583";
        module.Version = "RFC 8583";
        module.Dependencies.Add("rfc8581");
        module.Types.Add(RfcExtensionDictionaryHelpers.CreateEnumeratedType("Load-Type"));
        AddValues(module);
        AddAvps(module);
        return module;
    }

    internal static void AddValues(DiameterDictionaryModule module)
    {
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "HOST",
            0,
            "Load-Type",
            DiameterAvpCodes.LoadType));
        module.EnumValues.Add(RfcExtensionDictionaryHelpers.CreateValue(
            "PEER",
            1,
            "Load-Type",
            DiameterAvpCodes.LoadType));
    }

    internal static void AddAvps(DiameterDictionaryModule module)
    {
        DiameterAvpDefinition load = RfcExtensionDictionaryHelpers.CreateGrouped(
            DiameterAvpCodes.Load,
            "Load");
        RfcExtensionDictionaryHelpers.AddGroupedRule(load, DiameterAvpCodes.LoadType, "Load-Type", 0, 1);
        RfcExtensionDictionaryHelpers.AddGroupedRule(load, DiameterAvpCodes.LoadValue, "Load-Value", 0, 1);
        RfcExtensionDictionaryHelpers.AddGroupedRule(load, DiameterAvpCodes.SourceId, "SourceID", 0, 1);
        module.Avps.Add(load);
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.LoadType,
            "Load-Type",
            "Load-Type",
            DiameterAvpBaseType.Integer32));
        module.Avps.Add(RfcExtensionDictionaryHelpers.CreateAvp(
            DiameterAvpCodes.LoadValue,
            "Load-Value",
            "Unsigned64",
            DiameterAvpBaseType.Unsigned64));
    }
}
