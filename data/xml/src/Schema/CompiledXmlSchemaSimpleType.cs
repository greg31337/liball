namespace xml;

internal enum XmlSchemaSimpleVariety
{
    Universal = 0,
    Atomic = 1,
    List = 2,
    Union = 3
}

internal enum XmlSchemaOrderKind
{
    None = 0,
    Partial = 1,
    Total = 2
}

internal enum XmlSchemaFacetKind
{
    Length = 0,
    MinLength = 1,
    MaxLength = 2,
    Pattern = 3,
    Enumeration = 4,
    WhiteSpace = 5,
    MaxInclusive = 6,
    MaxExclusive = 7,
    MinExclusive = 8,
    MinInclusive = 9,
    TotalDigits = 10,
    FractionDigits = 11,
    Assertion = 12,
    ExplicitTimezone = 13,
    MaxScale = 14,
    MinScale = 15
}

internal class XmlSchemaFundamentalFacets
{
    internal XmlSchemaOrderKind Ordered;
    internal bool Bounded;
    internal bool CardinalityFinite;
    internal bool Numeric;

    internal XmlSchemaFundamentalFacets(XmlSchemaOrderKind ordered, bool bounded, bool cardinalityFinite, bool numeric)
    {
        Ordered = ordered;
        Bounded = bounded;
        CardinalityFinite = cardinalityFinite;
        Numeric = numeric;
    }
}

internal class CompiledXmlSchemaFacet
{
    internal XmlSchemaFacetKind Kind;
    internal string Value;
    internal bool Fixed;
    internal bool BuiltIn;
    internal XmlNode? Node;

    internal CompiledXmlSchemaFacet(XmlSchemaFacetKind kind, string value, bool fixedValue, bool builtIn, XmlNode? node)
    {
        Kind = kind;
        Value = value;
        Fixed = fixedValue;
        BuiltIn = builtIn;
        Node = node;
    }
}

internal class CompiledXmlSchemaSimpleType
{
    internal XmlName? Name;
    internal XmlSchemaSimpleVariety Variety;
    internal CompiledXmlSchemaSimpleType? BaseType;
    internal CompiledXmlSchemaSimpleType? ItemType;
    internal CompiledXmlSchemaSimpleType[] MemberTypes;
    internal CompiledXmlSchemaFacet[] DeclaredFacets;
    internal CompiledXmlSchemaFacet[] EffectiveFacets;
    internal XmlSchemaFundamentalFacets FundamentalFacets;
    internal XmlSchemaDerivationMethod DerivationMethod;
    internal XmlSchemaDerivationMethod Final;
    internal XdmAtomicType AtomicType;
    internal bool HasAtomicType;
    internal bool IsBuiltIn;
    internal bool IsAnonymous;
    internal bool IsCompiled;
    internal bool IsCompiling;
    internal XmlNode? Node;
    internal XmlSchemaModule? Module;
    internal CompiledXmlSchemaSimpleType? PreviousDefinition;

    internal CompiledXmlSchemaSimpleType(XmlName? name, bool builtIn, bool anonymous, XmlNode? node, XmlSchemaModule? module)
    {
        Name = name;
        Variety = XmlSchemaSimpleVariety.Universal;
        BaseType = null;
        ItemType = null;
        MemberTypes = [];
        DeclaredFacets = [];
        EffectiveFacets = [];
        FundamentalFacets = new XmlSchemaFundamentalFacets(XmlSchemaOrderKind.None, false, false, false);
        DerivationMethod = XmlSchemaDerivationMethod.None;
        Final = XmlSchemaDerivationMethod.None;
        AtomicType = XdmAtomicType.AnyAtomicType;
        HasAtomicType = false;
        IsBuiltIn = builtIn;
        IsAnonymous = anonymous;
        IsCompiled = builtIn;
        IsCompiling = false;
        Node = node;
        Module = module;
        PreviousDefinition = null;
    }
}
