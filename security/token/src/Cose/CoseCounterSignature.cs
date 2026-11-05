using System;
namespace token;

internal class CoseCounterSignatureItemResult
{
    internal bool Success;
    internal TokenError? Error;
    internal CborItem? Item;

    internal CoseCounterSignatureItemResult()
    {
        Success = false;
        Error = null;
        Item = null;
    }
}
