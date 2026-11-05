namespace asn1;

internal class XsdSimpleTypeUse
{
    internal string StableId = "";
    internal string Name = "";
    internal string NamespaceUri = "";
    internal string BuiltInTypeName = "";
    internal string AsnTypeDefinition = "";
    internal bool Anonymous;
    internal XsdSimpleTypeDefinition? Definition;
    internal AsnEncodingInstruction[] XerInstructions = [];
}
