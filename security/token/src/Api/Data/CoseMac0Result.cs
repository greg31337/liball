using System;
namespace token;

/// <summary>Contains the outcome of parsing an RFC 9052 COSE_Mac0 message.</summary>
public class CoseMac0Result
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed message, or <c>null</c> on failure.</summary>
    public CoseMac0? Message;

    /// <summary>Initializes an unsuccessful result with no parsed message.</summary>
    public CoseMac0Result()
    {
        Success = false;
        Error = null;
        Message = null;
    }
}
