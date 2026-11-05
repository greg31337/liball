namespace asn1;

internal class BerTlvSelectionResult
{
    internal bool Success;
    internal BerTlv? Value;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
