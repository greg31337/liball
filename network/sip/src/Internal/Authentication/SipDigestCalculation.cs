namespace sip;

internal class SipDigestCalculation
{
    internal SipDigestAlgorithm Algorithm;
    internal SipDigestQuality Quality;
    internal string? UserName;
    internal string? Realm;
    internal byte[]? Secret = new byte[0];
    internal string? Nonce;
    internal long NonceCount = -1;
    internal string? ClientNonce;
    internal string? Method;
    internal string? DigestUri;
    internal byte[]? RequestBody = new byte[0];
    internal byte[]? ResponseBody = new byte[0];
    internal bool Utf8;
}
