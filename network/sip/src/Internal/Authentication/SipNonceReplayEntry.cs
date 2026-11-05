namespace sip;

internal class SipNonceReplayEntry
{
    internal string Realm = "";
    internal string Identity = "";
    internal string Nonce = "";
    internal string ClientNonce = "";
    internal long HighestNonceCount;
    internal long ExpiresAtMilliseconds;
}
