using System;
namespace token;

/// <summary>Contains parsed full countersignatures from a COSE header.</summary>
public class CoseCounterSignatureCollectionResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains countersignatures in serialization order.</summary>
    public CoseCounterSignature[]? CounterSignatures;

    /// <summary>Initializes an unsuccessful result.</summary>
    public CoseCounterSignatureCollectionResult()
    {
        Success = false;
        Error = null;
        CounterSignatures = null;
    }
}
