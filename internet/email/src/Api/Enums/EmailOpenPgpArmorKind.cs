using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported openPGP armor values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailOpenPgpArmorKind
{
    /// <summary>
    /// Represents the message choice or state.
    /// </summary>
    Message = 0,
    /// <summary>
    /// Represents the public key choice or state.
    /// </summary>
    PublicKey = 1,
    /// <summary>
    /// Represents the private key choice or state.
    /// </summary>
    PrivateKey = 2,
    /// <summary>
    /// Represents the signature choice or state.
    /// </summary>
    Signature = 3
}
