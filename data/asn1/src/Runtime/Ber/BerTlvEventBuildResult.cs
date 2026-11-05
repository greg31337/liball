namespace asn1;

internal class BerTlvEventBuildResult
{
    internal bool Success;
    internal BerTlv? Root;
    internal System.Collections.Generic.Dictionary<BerTlv, long> ExpectedLengths =
        new System.Collections.Generic.Dictionary<BerTlv, long>();
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
