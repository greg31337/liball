using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported protected header values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailProtectedHeaderState
{
    /// <summary>
    /// Represents the unprotected choice or state.
    /// </summary>
    Unprotected = 0,
    /// <summary>
    /// Represents the signed only choice or state.
    /// </summary>
    SignedOnly = 1,
    /// <summary>
    /// Represents the encrypted only choice or state.
    /// </summary>
    EncryptedOnly = 2,
    /// <summary>
    /// Represents the signed and encrypted choice or state.
    /// </summary>
    SignedAndEncrypted = 3
}
