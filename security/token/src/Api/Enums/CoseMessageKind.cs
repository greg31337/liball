using System;
namespace token;

/// <summary>Identifies an RFC 9052 COSE message structure.</summary>
public enum CoseMessageKind
{
    /// <summary>No COSE message kind has been identified.</summary>
    Unknown = 0,

    /// <summary>A COSE_Encrypt0 message identified by CBOR tag 16.</summary>
    Encrypt0 = 16,

    /// <summary>A COSE_Mac0 message identified by CBOR tag 17.</summary>
    Mac0 = 17,

    /// <summary>A COSE_Sign1 message identified by CBOR tag 18.</summary>
    Sign1 = 18,

    /// <summary>A COSE_Encrypt message identified by CBOR tag 96.</summary>
    Encrypt = 96,

    /// <summary>A COSE_Mac message identified by CBOR tag 97.</summary>
    Mac = 97,

    /// <summary>A COSE_Sign message identified by CBOR tag 98.</summary>
    Sign = 98
}
