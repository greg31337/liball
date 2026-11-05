namespace xml;

internal enum XPathStaticItemKind
{
    Unknown = 0,
    AnyItem = 1,
    Node = 2,
    Atomic = 3,
    Function = 4,
    Map = 5,
    Array = 6
}

internal enum XPathStaticCardinality
{
    Empty = 0,
    ExactlyOne = 1,
    ZeroOrOne = 2,
    OneOrMore = 3,
    ZeroOrMore = 4
}

internal class XPathStaticType
{
    internal XPathStaticItemKind ItemKind;
    internal XPathStaticCardinality Cardinality;
    internal XdmAtomicType AtomicType;
    internal XdmNodeKind NodeKind;
    internal bool AnyNode;

    internal XPathStaticType(XPathStaticItemKind itemKind, XPathStaticCardinality cardinality)
    {
        ItemKind = itemKind;
        Cardinality = cardinality;
        AtomicType = XdmAtomicType.AnyAtomicType;
        NodeKind = XdmNodeKind.Document;
        AnyNode = false;
    }
}
