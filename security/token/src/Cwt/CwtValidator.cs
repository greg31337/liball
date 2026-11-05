using System;
namespace token;

internal class CwtLayerValidationResult
{
    internal bool Success;
    internal TokenError? Error;
    internal byte[]? Payload;
    internal CoseHeader? Header;
    internal CoseHeader[] RelatedHeaders;

    internal CwtLayerValidationResult()
    {
        Success = false;
        Error = null;
        Payload = null;
        Header = null;
        RelatedHeaders = Array.Empty<CoseHeader>();
    }
}

internal class CwtEnvelopeResult
{
    internal bool Success;
    internal TokenError? Error;
    internal byte[]? Message;
    internal bool Tagged;

    internal CwtEnvelopeResult()
    {
        Success = false;
        Error = null;
        Message = null;
        Tagged = false;
    }
}
