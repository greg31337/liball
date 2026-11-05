namespace asn1;

internal class EcnPredeclaredEncodingObject
{
    internal string StableId = "";
    internal string Name = "";
    internal string ClassName = "";
    internal string PrimitiveClassName = "";
    internal EcnClassCategory ClassCategory;
    internal EcnPredeclaredEncodingRule Rule;
    internal bool Shared;
    internal bool RequiresBounds;
    internal bool RequiresFixedSize;
    internal bool RequiresNoOptionalComponents;
    internal bool NoAdditionalEncoding;
}
