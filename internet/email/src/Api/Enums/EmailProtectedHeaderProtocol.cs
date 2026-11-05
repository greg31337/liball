using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported protected header protocol values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailProtectedHeaderProtocol
{
    /// <summary>
    /// Represents the S/MIME choice or state.
    /// </summary>
    Smime = 0,
    /// <summary>
    /// Represents the openPGP MIME choice or state.
    /// </summary>
    OpenPgpMime = 1
}
