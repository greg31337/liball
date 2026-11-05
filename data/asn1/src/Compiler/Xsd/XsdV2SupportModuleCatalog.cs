namespace asn1;

internal static class XsdV2SupportModuleCatalog
{
    internal static XsdV2SupportModule GetModule()
    {
        return new XsdV2SupportModule
        {
            StableId = "x694:module:xsd:version2",
            ModuleName = "XSDv2",
            ObjectIdentifier = [2, 1, 0, 0, 2, 2],
            InternationalizedResourceIdentifier =
                "/ASN.1/Specification/Modules/XSD-Module/Version2",
            Types = CreateTypes(),
            XerEncodingInstructions = CreateXerInstructions()
        };
    }

    internal static XsdV2SupportTypeDefinition[] CreateTypes()
    {
        return
        [
            CreateType("AnySimpleType", "anySimpleType", "XMLCompatibleString", XsdBuiltInTypeFamily.Any),
            CreateType("AnyType", "anyType", "SEQUENCE { embed-values SEQUENCE OF String, attr SEQUENCE OF String, elem-list SEQUENCE OF elem String }", XsdBuiltInTypeFamily.Any),
            CreateType("AnyType-nillable", "anyType", "SEQUENCE { embed-values SEQUENCE OF String, attr SEQUENCE OF String, content SEQUENCE { elem-list SEQUENCE OF elem String } OPTIONAL }", XsdBuiltInTypeFamily.Any),
            CreateType("AnyURI", "anyURI", "XMLStringWithNoCRLFHT (CONSTRAINED BY { /* valid URI or IRI */ })", XsdBuiltInTypeFamily.QNameUri),
            CreateType("Date", "date", "GenericTimeTypeChoice { TIME (SETTINGS \"Basic=Date Date=YMD\"), VisibleString (FROM (\"0\"..\"9\" | \"DHMPSTY:.+-\")) }", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("DateTime", "dateTime", "TIME (SETTINGS \"Basic=Date-Time Date=YMD Midnight=Start\")", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("Decimal", "decimal", "REAL (0 | WITH COMPONENTS {..., base(10)})", XsdBuiltInTypeFamily.Numeric),
            CreateType("Double", "double", "REAL (0 | MINUS-INFINITY | PLUS-INFINITY | NOT-A-NUMBER | WITH COMPONENTS { mantissa(-9007199254740991..9007199254740991), base(2), exponent(-1074..971) })", XsdBuiltInTypeFamily.Numeric),
            CreateType("Duration", "duration", "GenericTimeTypeChoice { DURATION, VisibleString (FROM (\"0\"..\"9\" | \"DHMPSTY:.+-\")) }", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("ENTITIES", "ENTITIES", "SEQUENCE (SIZE(1..MAX)) OF ENTITY", XsdBuiltInTypeFamily.NameToken),
            CreateType("ENTITY", "ENTITY", "NCName", XsdBuiltInTypeFamily.NameToken),
            CreateType("Float", "float", "REAL (0 | MINUS-INFINITY | PLUS-INFINITY | NOT-A-NUMBER | WITH COMPONENTS { mantissa(-16777215..16777215), base(2), exponent(-149..104) })", XsdBuiltInTypeFamily.Numeric),
            CreateType("GDay", "gDay", "DateTimeType (Day)", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("GMonth", "gMonth", "DateTimeType (Month)", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("GMonthDay", "gMonthDay", "DateTimeType (MonthDay)", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("GYear", "gYear", "GenericTimeTypeChoice { TIME (SETTINGS \"Basic=Date Date=Y\"), VisibleString (FROM (\"0\"..\"9\" | \"Z:+-\")) }", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("GYearMonth", "gYearMonth", "GenericTimeTypeChoice { TIME (SETTINGS \"Basic=Date Date=YM\"), VisibleString (FROM (\"0\"..\"9\" | \"Z:+-\")) }", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("ID", "ID", "NCName", XsdBuiltInTypeFamily.NameToken),
            CreateType("IDREF", "IDREF", "NCName", XsdBuiltInTypeFamily.NameToken),
            CreateType("IDREFS", "IDREFS", "SEQUENCE (SIZE(1..MAX)) OF IDREF", XsdBuiltInTypeFamily.NameToken),
            CreateType("Int", "int", "INTEGER (-2147483648..2147483647)", XsdBuiltInTypeFamily.Numeric),
            CreateType("Language", "language", "VisibleString (FROM (\"a\"..\"z\" | \"A\"..\"Z\" | \"-\" | \"0\"..\"9\")) (PATTERN \"[a-zA-Z]#(1,8)(-[a-zA-Z0-9]#(1,8))*\")", XsdBuiltInTypeFamily.NameToken),
            CreateType("Long", "long", "INTEGER (-9223372036854775808..9223372036854775807)", XsdBuiltInTypeFamily.Numeric),
            CreateType("Name", "Name", "Token (XMLStringWithNoWhitespace) (CONSTRAINED BY { /* XML Name */ })", XsdBuiltInTypeFamily.NameToken),
            CreateType("NCName", "NCName", "Name (CONSTRAINED BY { /* XML NCName */ })", XsdBuiltInTypeFamily.NameToken),
            CreateType("NMTOKEN", "NMTOKEN", "Token (XMLStringWithNoWhitespace) (CONSTRAINED BY { /* XML NMTOKEN */ })", XsdBuiltInTypeFamily.NameToken),
            CreateType("NMTOKENS", "NMTOKENS", "SEQUENCE (SIZE(1..MAX)) OF NMTOKEN", XsdBuiltInTypeFamily.NameToken),
            CreateType("NormalizedString", "normalizedString", "String (XMLStringWithNoCRLFHT)", XsdBuiltInTypeFamily.String),
            CreateType("NOTATION", "NOTATION", "QName", XsdBuiltInTypeFamily.QNameUri),
            CreateType("QName", "QName", "SEQUENCE { uri AnyURI OPTIONAL, name NCName }", XsdBuiltInTypeFamily.QNameUri),
            CreateType("Short", "short", "INTEGER (-32768..32767)", XsdBuiltInTypeFamily.Numeric),
            CreateType("String", "string", "XMLCompatibleString", XsdBuiltInTypeFamily.String),
            CreateType("Time", "time", "TIME ((SETTINGS \"Basic=Time\") EXCEPT (SETTINGS \"Midnight=End\"))", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("Token", "token", "NormalizedString (CONSTRAINED BY { /* XSD token */ })", XsdBuiltInTypeFamily.NameToken),
            CreateType("UnsignedInt", "unsignedInt", "INTEGER (0..4294967295)", XsdBuiltInTypeFamily.Numeric),
            CreateType("UnsignedLong", "unsignedLong", "INTEGER (0..18446744073709551615)", XsdBuiltInTypeFamily.Numeric),
            CreateType("UnsignedShort", "unsignedShort", "INTEGER (0..65535)", XsdBuiltInTypeFamily.Numeric),
            CreateType("XMLCompatibleString", "", "UTF8String (FROM ({0,0,0,9} | {0,0,0,10} | {0,0,0,13} | {0,0,0,32}..{0,0,215,255} | {0,0,224,0}..{0,0,255,253} | {0,1,0,0}..{0,16,255,253}))", XsdBuiltInTypeFamily.String),
            CreateType("XMLStringWithNoWhitespace", "", "UTF8String (FROM ({0,0,0,33}..{0,0,215,255} | {0,0,224,0}..{0,0,255,253} | {0,1,0,0}..{0,16,255,253}))", XsdBuiltInTypeFamily.String),
            CreateType("XMLStringWithNoCRLFHT", "", "UTF8String (FROM ({0,0,0,32}..{0,0,215,255} | {0,0,224,0}..{0,0,255,253} | {0,1,0,0}..{0,16,255,253}))", XsdBuiltInTypeFamily.String),
            CreateType("GenericTimeTypeChoice", "", "{BasicType, Alternative} CHOICE { asn1supportedvalue BasicType, othervalues Alternative }", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("DateTimeType", "", "VisibleString (FROM (\"0\"..\"9\" | \"TZ:.+-\"))", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("Day", "", "DateTimeType (FROM (\"0\"..\"9\" | \"Z:+-\"))", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("Month", "", "DateTimeType (FROM (\"0\"..\"9\" | \"Z:+-\"))", XsdBuiltInTypeFamily.DateTimeDuration),
            CreateType("MonthDay", "", "DateTimeType (FROM (\"0\"..\"9\" | \"Z:+-\"))", XsdBuiltInTypeFamily.DateTimeDuration)
        ];
    }

    internal static string[] CreateXerInstructions()
    {
        return
        [
            "GLOBAL-DEFAULTS MODIFIED-ENCODINGS",
            "GLOBAL-DEFAULTS CONTROL-NAMESPACE http://www.w3.org/2001/XMLSchema-instance PREFIX xsi",
            "NAMESPACE ALL, ALL IN ALL AS http://www.w3.org/2001/XMLSchema PREFIX xsd",
            "USE-QNAME QName",
            "DECIMAL Decimal",
            "LIST ENTITIES, IDREFS, NMTOKENS",
            "EMBED-VALUES AnyType, AnyType-nillable",
            "ANY-ATTRIBUTES AnyType.attr, AnyType-nillable.attr",
            "ANY-ELEMENT AnyType.elem-list.*, AnyType-nillable.content.elem-list.*",
            "UNTAGGED AnyType.elem-list, AnyType-nillable.content.elem-list",
            "NAME built-in-support-types AS UNCAPITALIZED",
            "NAME GenericTimeTypeChoice.ALL AS empty",
            "USE-NIL AnyType-nillable",
            "USE-UNION GenericTimeTypeChoice",
            "WHITESPACE AnyURI, Language, Token, DateTimeType COLLAPSE",
            "WHITESPACE NormalizedString REPLACE"
        ];
    }

    internal static XsdV2SupportTypeDefinition CreateType(
        string name,
        string xsdBuiltInName,
        string definition,
        XsdBuiltInTypeFamily family)
    {
        return new XsdV2SupportTypeDefinition
        {
            StableId = "x694:module:xsd:version2:type:" + name,
            Name = name,
            XsdBuiltInName = xsdBuiltInName,
            Definition = definition,
            Family = family
        };
    }
}
