namespace asn1;

internal class BerComponentValue
{
    internal AsnFieldDescriptor? Field;
    internal bool Present;
    internal bool EqualsDefault;
    internal BerTlv? Tlv;
}
