using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Specifies the supported openPGP signed values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailOpenPgpSignedFormat
{
    /// <summary>
    /// Represents the pgp MIME choice or state.
    /// </summary>
    PgpMime = 0,
    /// <summary>
    /// Represents the inline choice or state.
    /// </summary>
    Inline = 1,
    /// <summary>
    /// Represents the cleartext choice or state.
    /// </summary>
    Cleartext = 2
}
