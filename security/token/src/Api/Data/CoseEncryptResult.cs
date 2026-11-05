using System;
using System.Collections.Generic;

namespace token;

/// <summary>Contains the outcome of parsing an RFC 9052 COSE_Encrypt message.</summary>
public class CoseEncryptResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed message, or <c>null</c> on failure.</summary>
    public CoseEncrypt? Message;

    /// <summary>Initializes an unsuccessful result with no parsed message.</summary>
    public CoseEncryptResult()
    {
        Success = false;
        Error = null;
        Message = null;
    }
}
