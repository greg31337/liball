namespace asn1;

internal class PerCollectionResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal bool ExtensionPresent;
    internal bool[] RootPresence = [];
    internal bool[] ExtensionPresence = [];
    internal byte[][] ExtensionData = [];
    internal int[] ExtensionBitOffsets = [];
    internal int[] ExtensionBitCounts = [];
    internal int[] ExtensionAlignmentPaddingBitCounts = [];
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
