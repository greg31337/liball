namespace asn1;

internal class EcnPlanBufferRequirement
{
    internal long MinimumBits;
    internal long MinimumBytes;
    internal bool RequiresWholeValue;
    internal bool RequiresLookahead;
    internal bool RequiresBackpatch;
    internal bool RequiresRandomAccess;
    internal bool Streamable = true;
}
