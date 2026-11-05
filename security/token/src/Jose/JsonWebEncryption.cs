using System;
using System.Security.Cryptography;

namespace token;

internal class JweKeyManagementResult
{
    internal bool Success;
    internal TokenError? Error;
    internal byte[]? ContentEncryptionKey;
    internal byte[]? EncryptedKey;

    internal JweKeyManagementResult()
    {
        Success = false;
        Error = null;
        ContentEncryptionKey = null;
        EncryptedKey = null;
    }
}

internal class JweIntegerResult
{
    internal bool Success;
    internal TokenError? Error;
    internal int Value;

    internal JweIntegerResult()
    {
        Success = false;
        Error = null;
        Value = 0;
    }
}
