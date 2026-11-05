namespace asn1;

internal static class XsdBuiltInTypeCatalog
{
    internal const string SchemaNamespace =
        "http://www.w3.org/2001/XMLSchema";
    internal static readonly XsdBuiltInTypeDefinition[] Definitions =
        CreateDefinitions();

    internal static XsdBuiltInTypeDefinition[] GetTypes()
    {
        XsdBuiltInTypeDefinition[] result =
            new XsdBuiltInTypeDefinition[Definitions.Length];
        int index = 0;
        while (index < Definitions.Length)
        {
            result[index] = Definitions[index];
            index = index + 1;
        }
        return result;
    }

    internal static XsdBuiltInTypeDefinition? FindType(
        string namespaceUri,
        string localName)
    {
        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }
        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }
        if (namespaceUri != SchemaNamespace)
        {
            return null;
        }
        int index = 0;
        while (index < Definitions.Length)
        {
            if (Definitions[index].XsdName == localName)
            {
                return Definitions[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static string MapType(
        string namespaceUri,
        string localName,
        bool nillable)
    {
        XsdBuiltInTypeDefinition? definition = FindType(
            namespaceUri,
            localName);
        if (definition == null)
        {
            return "";
        }
        if (nillable && definition.NillableAsnTypeDefinition.Length > 0)
        {
            return definition.NillableAsnTypeDefinition;
        }
        return definition.AsnTypeDefinition;
    }

    internal static XsdBuiltInTypeDefinition[] CreateDefinitions()
    {
        return
        [
            CreateSupport("anyURI", "AnyURI", XsdBuiltInTypeFamily.QNameUri, ["WHITESPACE COLLAPSE"]),
            CreateSupport("anySimpleType", "AnySimpleType", XsdBuiltInTypeFamily.Any, []),
            CreateAnyType(),
            CreateDirect("base64Binary", "[BASE64] OCTET STRING", XsdBuiltInTypeFamily.Binary, ["BASE64"]),
            CreateDirect("boolean", "BOOLEAN", XsdBuiltInTypeFamily.Boolean, []),
            CreateDirect("byte", "INTEGER (-128..127)", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("date", "Date", XsdBuiltInTypeFamily.DateTimeDuration, ["USE-UNION"]),
            CreateSupport("dateTime", "DateTime", XsdBuiltInTypeFamily.DateTimeDuration, []),
            CreateSupport("decimal", "Decimal", XsdBuiltInTypeFamily.Numeric, ["DECIMAL"]),
            CreateSupport("double", "Double", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("duration", "Duration", XsdBuiltInTypeFamily.DateTimeDuration, ["USE-UNION"]),
            CreateSupport("ENTITIES", "ENTITIES", XsdBuiltInTypeFamily.NameToken, ["LIST"]),
            CreateSupport("ENTITY", "ENTITY", XsdBuiltInTypeFamily.NameToken, []),
            CreateSupport("float", "Float", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("gDay", "GDay", XsdBuiltInTypeFamily.DateTimeDuration, []),
            CreateSupport("gMonth", "GMonth", XsdBuiltInTypeFamily.DateTimeDuration, []),
            CreateSupport("gMonthDay", "GMonthDay", XsdBuiltInTypeFamily.DateTimeDuration, []),
            CreateSupport("gYear", "GYear", XsdBuiltInTypeFamily.DateTimeDuration, ["USE-UNION"]),
            CreateSupport("gYearMonth", "GYearMonth", XsdBuiltInTypeFamily.DateTimeDuration, ["USE-UNION"]),
            CreateDirect("hexBinary", "OCTET STRING", XsdBuiltInTypeFamily.Binary, []),
            CreateSupport("ID", "ID", XsdBuiltInTypeFamily.NameToken, []),
            CreateSupport("IDREF", "IDREF", XsdBuiltInTypeFamily.NameToken, []),
            CreateSupport("IDREFS", "IDREFS", XsdBuiltInTypeFamily.NameToken, ["LIST"]),
            CreateSupport("int", "Int", XsdBuiltInTypeFamily.Numeric, []),
            CreateDirect("integer", "INTEGER", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("language", "Language", XsdBuiltInTypeFamily.NameToken, ["WHITESPACE COLLAPSE"]),
            CreateSupport("long", "Long", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("Name", "Name", XsdBuiltInTypeFamily.NameToken, []),
            CreateSupport("NCName", "NCName", XsdBuiltInTypeFamily.NameToken, []),
            CreateDirect("negativeInteger", "INTEGER (MIN..-1)", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("NMTOKEN", "NMTOKEN", XsdBuiltInTypeFamily.NameToken, []),
            CreateSupport("NMTOKENS", "NMTOKENS", XsdBuiltInTypeFamily.NameToken, ["LIST"]),
            CreateDirect("nonNegativeInteger", "INTEGER (0..MAX)", XsdBuiltInTypeFamily.Numeric, []),
            CreateDirect("nonPositiveInteger", "INTEGER (MIN..0)", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("normalizedString", "NormalizedString", XsdBuiltInTypeFamily.String, ["WHITESPACE REPLACE"]),
            CreateSupport("NOTATION", "NOTATION", XsdBuiltInTypeFamily.QNameUri, ["USE-QNAME"]),
            CreateDirect("positiveInteger", "INTEGER (1..MAX)", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("QName", "QName", XsdBuiltInTypeFamily.QNameUri, ["USE-QNAME"]),
            CreateSupport("short", "Short", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("string", "String", XsdBuiltInTypeFamily.String, []),
            CreateSupport("time", "Time", XsdBuiltInTypeFamily.DateTimeDuration, []),
            CreateSupport("token", "Token", XsdBuiltInTypeFamily.NameToken, ["WHITESPACE COLLAPSE"]),
            CreateDirect("unsignedByte", "INTEGER (0..255)", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("unsignedInt", "UnsignedInt", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("unsignedLong", "UnsignedLong", XsdBuiltInTypeFamily.Numeric, []),
            CreateSupport("unsignedShort", "UnsignedShort", XsdBuiltInTypeFamily.Numeric, [])
        ];
    }

    internal static XsdBuiltInTypeDefinition CreateAnyType()
    {
        XsdBuiltInTypeDefinition result = CreateSupport(
            "anyType",
            "AnyType",
            XsdBuiltInTypeFamily.Any,
            ["EMBED-VALUES", "ANY-ATTRIBUTES", "ANY-ELEMENT"]);
        result.NillableAsnTypeDefinition = "XSDv2.AnyType-nillable";
        return result;
    }

    internal static XsdBuiltInTypeDefinition CreateSupport(
        string xsdName,
        string supportTypeName,
        XsdBuiltInTypeFamily family,
        string[] instructions)
    {
        XsdBuiltInTypeDefinition result = CreateDirect(
            xsdName,
            "XSDv2." + supportTypeName,
            family,
            instructions);
        result.SupportTypeName = supportTypeName;
        return result;
    }

    internal static XsdBuiltInTypeDefinition CreateDirect(
        string xsdName,
        string asnTypeDefinition,
        XsdBuiltInTypeFamily family,
        string[] instructions)
    {
        return new XsdBuiltInTypeDefinition
        {
            StableId = "x694:built-in:" + xsdName,
            XsdName = xsdName,
            AsnTypeDefinition = asnTypeDefinition,
            Family = family,
            XerInstructions = instructions
        };
    }
}
