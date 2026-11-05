using System;
using System.Security.Cryptography;

namespace token;

internal class CoseContentEncryptionResult
{
    internal bool Success;
    internal TokenError? Error;
    internal byte[]? Ciphertext;
    internal byte[]? InitializationVector;

    internal CoseContentEncryptionResult()
    {
        Success = false;
        Error = null;
        Ciphertext = null;
        InitializationVector = null;
    }
}
