using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported openPGP packet values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailOpenPgpPacketKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the public key encrypted session key choice or state.
    /// </summary>
    PublicKeyEncryptedSessionKey = 1,
    /// <summary>
    /// Represents the signature choice or state.
    /// </summary>
    Signature = 2,
    /// <summary>
    /// Represents the symmetric key encrypted session key choice or state.
    /// </summary>
    SymmetricKeyEncryptedSessionKey = 3,
    /// <summary>
    /// Represents the one pass signature choice or state.
    /// </summary>
    OnePassSignature = 4,
    /// <summary>
    /// Represents the secret key choice or state.
    /// </summary>
    SecretKey = 5,
    /// <summary>
    /// Represents the public key choice or state.
    /// </summary>
    PublicKey = 6,
    /// <summary>
    /// Represents the secret subkey choice or state.
    /// </summary>
    SecretSubkey = 7,
    /// <summary>
    /// Represents the compressed data choice or state.
    /// </summary>
    CompressedData = 8,
    /// <summary>
    /// Represents the symmetrically encrypted data choice or state.
    /// </summary>
    SymmetricallyEncryptedData = 9,
    /// <summary>
    /// Represents the marker choice or state.
    /// </summary>
    Marker = 10,
    /// <summary>
    /// Represents the literal data choice or state.
    /// </summary>
    LiteralData = 11,
    /// <summary>
    /// Represents the trust choice or state.
    /// </summary>
    Trust = 12,
    /// <summary>
    /// Represents the user ID choice or state.
    /// </summary>
    UserId = 13,
    /// <summary>
    /// Represents the public subkey choice or state.
    /// </summary>
    PublicSubkey = 14,
    /// <summary>
    /// Represents the user attribute choice or state.
    /// </summary>
    UserAttribute = 17,
    /// <summary>
    /// Represents the symmetrically encrypted integrity protected data choice or state.
    /// </summary>
    SymmetricallyEncryptedIntegrityProtectedData = 18,
    /// <summary>
    /// Represents the modification detection code choice or state.
    /// </summary>
    ModificationDetectionCode = 19,
    /// <summary>
    /// Represents the aead encrypted data choice or state.
    /// </summary>
    AeadEncryptedData = 20,
    /// <summary>
    /// Represents the padding choice or state.
    /// </summary>
    Padding = 21
}
