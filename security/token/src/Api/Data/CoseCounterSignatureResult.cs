using System;
namespace token;

/// <summary>Contains the outcome of parsing one RFC 9338 full countersignature.</summary>
public class CoseCounterSignatureResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed countersignature, or <c>null</c> on failure.</summary>
    public CoseCounterSignature? CounterSignature;

    /// <summary>Initializes an unsuccessful result.</summary>
    public CoseCounterSignatureResult()
    {
        Success = false;
        Error = null;
        CounterSignature = null;
    }
}
