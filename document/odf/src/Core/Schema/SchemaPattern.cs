namespace odf;

internal enum SchemaPatternKind
{
    Unknown = 0,
    Choice = 1,
    Group = 2,
    Interleave = 3,
    Optional = 4,
    ZeroOrMore = 5,
    OneOrMore = 6,
    Mixed = 7,
    List = 8,
    Element = 9,
    Attribute = 10,
    Ref = 11,
    ParentRef = 12,
    Data = 13,
    Value = 14,
    Text = 15,
    Empty = 16,
    NotAllowed = 17,
    AnyName = 18,
    NamespaceName = 19,
    Name = 20,
    Except = 21,
    ExternalRef = 22,
    Include = 23,
    Param = 24
}

internal class SchemaPattern
{
    internal SchemaPatternKind Kind;
    internal string Name = "";
    internal string Value = "";
    internal string NamespaceName = "";
    internal string DatatypeLibrary = "";
    internal int OwnerRuleIndex;
    internal int FirstChildIndex;
    internal int ChildCount;
}
