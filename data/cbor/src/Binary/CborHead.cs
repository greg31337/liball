namespace cbor;

internal class CborHead
{
    internal int MajorType;
    internal int AdditionalInformation;
    internal ulong Argument;
    internal int ArgumentWidth;
    internal int HeadLength;
    internal bool IsIndefinite;
    internal bool IsBreak;
}
