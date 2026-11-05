using System;
namespace token;

/// <summary>Contains the outcome of detecting a tagged or contextual untagged COSE message.</summary>
public class CoseMessageResult
{
    /// <summary>Indicates whether detection succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when detection succeeded.</summary>
    public TokenError? Error;

    /// <summary>Identifies the detected COSE message structure.</summary>
    public CoseMessageKind Kind;

    /// <summary>Indicates whether the message used its registered CBOR tag.</summary>
    public bool Tagged;

    /// <summary>Contains the registered CBOR tag, or zero for an untagged message.</summary>
    public ulong Tag;

    /// <summary>Contains the decoded untagged message array, or <c>null</c> on failure.</summary>
    public CborItem? Item;

    /// <summary>Initializes an unsuccessful result with no detected message.</summary>
    public CoseMessageResult()
    {
        Success = false;
        Error = null;
        Kind = CoseMessageKind.Unknown;
        Tagged = false;
        Tag = 0;
        Item = null;
    }
}
