using System;
using System.Security.Cryptography;

namespace token;

internal class CoseEncrypt0EncryptionResult
{
    internal bool Success;
    internal TokenError? Error;
    internal byte[]? Ciphertext;
    internal byte[]? InitializationVector;

    internal CoseEncrypt0EncryptionResult()
    {
        Success = false;
        Error = null;
        Ciphertext = null;
        InitializationVector = null;
    }
}
