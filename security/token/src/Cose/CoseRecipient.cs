using System;
namespace token;

internal class CoseRecipientEncoding
{
    internal bool Success;
    internal TokenError? Error;
    internal CborItem? Item;
    internal byte[]? ProtectedBytes;
    internal CoseRecipientEncoding[]? Children;

    internal CoseRecipientEncoding()
    {
        Success = false;
        Error = null;
        Item = null;
        ProtectedBytes = null;
        Children = null;
    }
}
