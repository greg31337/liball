namespace asn1;

internal class EcnFieldPathSegment
{
    internal string Name = "";
    internal EcnEncodingField? EncodingField;
    internal AsnComponent? AsnComponent;
    internal int FieldOrder = -1;
    internal bool Resolved;
}
