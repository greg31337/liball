namespace asn1;

internal class BerPrimitiveValueResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal byte[] Content = [];
    internal AsnRuntimeValue? Value;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
