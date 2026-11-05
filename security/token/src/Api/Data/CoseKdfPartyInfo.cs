using System;
namespace token;

/// <summary>Contains one party-information triple for a COSE KDF context.</summary>
public class CoseKdfPartyInfo
{
    /// <summary>Contains the party identity bytes, or <c>null</c> to encode CBOR null.</summary>
    public byte[]? Identity;

    /// <summary>Contains a byte-string or integer nonce, or <c>null</c> to encode CBOR null.</summary>
    public CborItem? Nonce;

    /// <summary>Contains protocol-defined party information, or <c>null</c> to encode CBOR null.</summary>
    public byte[]? Other;

    /// <summary>Initializes party information whose three fields encode as CBOR null.</summary>
    public CoseKdfPartyInfo()
    {
        Identity = null;
        Nonce = null;
        Other = null;
    }
}
