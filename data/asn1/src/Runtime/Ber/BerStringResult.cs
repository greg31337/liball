namespace asn1;

internal class BerStringResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal BerTlv? Tlv;
    internal AsnRuntimeValue? Value;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
