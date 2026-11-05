using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

internal class JweFlattenedContentResult
{
    internal bool Success;
    internal TokenError? Error;
    internal byte[]? InitializationVector;
    internal byte[]? Ciphertext;
    internal byte[]? AuthenticationTag;

    internal JweFlattenedContentResult()
    {
        Success = false;
        Error = null;
        InitializationVector = null;
        Ciphertext = null;
        AuthenticationTag = null;
    }
}
