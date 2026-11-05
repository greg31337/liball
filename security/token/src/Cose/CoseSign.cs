using System;
namespace token;

internal class CoseSignatureResult
{
    internal bool Success;
    internal TokenError? Error;
    internal CoseSignature? Signature;

    internal CoseSignatureResult()
    {
        Success = false;
        Error = null;
        Signature = null;
    }
}
