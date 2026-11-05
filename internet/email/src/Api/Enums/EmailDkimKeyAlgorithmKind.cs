using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported DKIM key algorithm values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDkimKeyAlgorithmKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the RSA choice or state.
    /// </summary>
    Rsa = 1,
    /// <summary>
    /// Represents the ed 25519 choice or state.
    /// </summary>
    Ed25519 = 2,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 3
}
