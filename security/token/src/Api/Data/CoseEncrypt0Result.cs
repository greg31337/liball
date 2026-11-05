using System;
namespace token;

/// <summary>Contains the outcome of parsing an RFC 9052 COSE_Encrypt0 message.</summary>
public class CoseEncrypt0Result
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed message, or <c>null</c> on failure.</summary>
    public CoseEncrypt0? Message;

    /// <summary>Initializes an unsuccessful result with no parsed message.</summary>
    public CoseEncrypt0Result()
    {
        Success = false;
        Error = null;
        Message = null;
    }
}
