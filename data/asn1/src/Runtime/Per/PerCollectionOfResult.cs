namespace asn1;

internal class PerCollectionOfResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal byte[][] ElementData = [];
    internal int[] ElementBitCounts = [];
    internal int[] FragmentLengths = [];
    internal int[] Order = [];
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
