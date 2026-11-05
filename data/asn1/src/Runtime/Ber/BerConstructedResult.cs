namespace asn1;

internal class BerConstructedResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal BerTlv? Tlv;
    internal BerTlv[] Children = [];
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
