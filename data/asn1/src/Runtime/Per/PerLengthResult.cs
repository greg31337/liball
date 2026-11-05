namespace asn1;

internal class PerLengthResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal int Length;
    internal int FragmentLength;
    internal bool MoreFragments;
    internal bool DeterminantOmitted;
    internal bool NormallySmall;
    internal int BitCount;
    internal int PaddingBitCount;
    internal byte[] Data = [];
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
