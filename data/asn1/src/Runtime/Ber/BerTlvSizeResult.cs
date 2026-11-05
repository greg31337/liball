namespace asn1;

internal class BerTlvSizeResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal long EncodedLength;
    internal System.Collections.Generic.Dictionary<BerTlv, BerTlvSizeInfo> Sizes =
        new System.Collections.Generic.Dictionary<BerTlv, BerTlvSizeInfo>();
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
