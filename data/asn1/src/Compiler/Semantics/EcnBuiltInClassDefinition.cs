namespace asn1;

internal class EcnBuiltInClassDefinition
{
    internal string StableId = "";
    internal string Name = "";
    internal string DerivedFromName = "";
    internal string PrimitiveName = "";
    internal EcnClassCategory Category;
    internal EcnClassCategoryGroup[] CategoryGroups = [];
    internal bool Primitive;
    internal bool EncodingStructure;
}
