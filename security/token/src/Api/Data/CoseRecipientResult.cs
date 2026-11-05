using System;
namespace token;

/// <summary>Contains the outcome of parsing one COSE recipient structure.</summary>
public class CoseRecipientResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when parsing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed recipient, or <c>null</c> when parsing failed.</summary>
    public CoseRecipient? Recipient;

    /// <summary>Initializes an unsuccessful result with no recipient.</summary>
    public CoseRecipientResult()
    {
        Success = false;
        Error = null;
        Recipient = null;
    }
}
