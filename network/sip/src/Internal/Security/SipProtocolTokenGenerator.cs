namespace sip;

internal class SipProtocolTokenGenerator
{
    const int IdentifierByteCount = 16;
    const int DigestTokenByteCount = 24;
    const int BoundaryByteCount = 18;
    const int FlowTokenByteCount = 24;
    SipRandomTokenGenerator tokenGenerator;

    internal SipProtocolTokenGenerator(ISipRandomSource randomSource)
    {
        tokenGenerator = new SipRandomTokenGenerator(randomSource);
    }

    internal string CreateBranch()
    {
        return "z9hG4bK" + tokenGenerator.CreateHexToken(IdentifierByteCount);
    }

    internal string CreateTag()
    {
        return tokenGenerator.CreateHexToken(IdentifierByteCount);
    }

    internal string CreateSessionId()
    {
        return tokenGenerator.CreateHexToken(IdentifierByteCount);
    }

    internal string CreateCallId()
    {
        return tokenGenerator.CreateHexToken(IdentifierByteCount);
    }

    internal string CreateClientNonce()
    {
        return tokenGenerator.CreateBase64UrlToken(DigestTokenByteCount);
    }

    internal string CreateMimeBoundary()
    {
        return "sip-" + tokenGenerator.CreateHexToken(BoundaryByteCount);
    }

    internal string CreateFlowToken()
    {
        return tokenGenerator.CreateBase64UrlToken(FlowTokenByteCount);
    }

    internal string CreateOpaqueToken()
    {
        return tokenGenerator.CreateBase64UrlToken(DigestTokenByteCount);
    }
}
