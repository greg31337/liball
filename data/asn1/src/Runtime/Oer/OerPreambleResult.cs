namespace asn1;

internal class OerPreambleResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal bool ExtensionPresent;
    internal bool[] Presence = [];
    internal byte[] Data = [];
    internal int ConsumedCount;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
