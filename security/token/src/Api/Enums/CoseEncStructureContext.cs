using System;
namespace token;

/// <summary>Identifies an RFC 9052 Enc_structure context.</summary>
public enum CoseEncStructureContext
{
    /// <summary>No Enc_structure operation context has been selected.</summary>
    Unknown = 0,

    /// <summary>The content-encryption layer of a COSE_Encrypt message.</summary>
    Encrypt = 1,

    /// <summary>A COSE_Encrypt0 message.</summary>
    Encrypt0 = 2,

    /// <summary>A recipient placed in a COSE_Encrypt message.</summary>
    EncryptRecipient = 3,

    /// <summary>A recipient placed in a COSE_Mac message.</summary>
    MacRecipient = 4,

    /// <summary>A recipient nested in another recipient.</summary>
    Recipient = 5
}
