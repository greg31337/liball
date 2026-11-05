using System;
namespace token;

/// <summary>Represents one RFC 9052 COSE_Signature entry.</summary>
public class CoseSignature
{
    /// <summary>Contains the signer protected and unprotected headers.</summary>
    public CoseHeader? Header;

    /// <summary>Contains the signature bytes.</summary>
    public byte[]? Signature;

    /// <summary>Indicates whether this signature has been cryptographically verified.</summary>
    public bool SignatureVerified;

    /// <summary>Initializes an empty signature entry.</summary>
    public CoseSignature()
    {
        Header = new CoseHeader();
        Signature = Array.Empty<byte>();
        SignatureVerified = false;
    }
}
