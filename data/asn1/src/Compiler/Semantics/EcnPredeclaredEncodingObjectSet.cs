namespace asn1;

internal class EcnPredeclaredEncodingObjectSet
{
    internal string StableId = "";
    internal string Name = "";
    internal string ObjectIdentifier = "";
    internal int[] ObjectIdentifierArcs = [];
    internal EcnPredeclaredEncodingRule Rule;
    internal EcnPredeclaredEncodingObject[] Objects = [];
    internal bool Per;
    internal bool Aligned;
    internal bool Canonical;
}
