namespace diameter;

internal static class RfcExtensionDictionaryHelpers
{
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

    internal static DiameterAvpDefinition CreateAvp(
        uint code,
        string name,
        string typeName,
        DiameterAvpBaseType baseType)
    {
        DiameterAvpDefinition avp = new DiameterAvpDefinition();
        avp.Code = code;
        avp.VendorId = DiameterVendorIds.Ietf;
        avp.Name = name;
        avp.TypeName = typeName;
        avp.BaseType = baseType;
        avp.ProhibitedFlags = DiameterAvpFlags.VendorSpecific;
        avp.FixedDataLength = FindFixedLength(baseType);
        return avp;
    }

    internal static DiameterAvpDefinition CreateGrouped(uint code, string name)
    {
        DiameterAvpDefinition avp = CreateAvp(
            code,
            name,
            "Grouped",
            DiameterAvpBaseType.Grouped);
        avp.AllowUnlistedAvps = true;
        return avp;
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

    internal static int FindFixedLength(DiameterAvpBaseType baseType)
    {
        if (baseType == DiameterAvpBaseType.Integer32 ||
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
}
