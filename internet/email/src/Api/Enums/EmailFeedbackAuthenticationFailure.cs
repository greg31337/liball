using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Specifies the supported feedback authentication failure values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailFeedbackAuthenticationFailure
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the adsp choice or state.
    /// </summary>
    Adsp = 1,
    /// <summary>
    /// Represents the body hash choice or state.
    /// </summary>
    BodyHash = 2,
    /// <summary>
    /// Represents the revoked choice or state.
    /// </summary>
    Revoked = 3,
    /// <summary>
    /// Represents the signature choice or state.
    /// </summary>
    Signature = 4,
    /// <summary>
    /// Represents the spf choice or state.
    /// </summary>
    Spf = 5,
    /// <summary>
    /// Represents the dmarc choice or state.
    /// </summary>
    Dmarc = 6,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 7
}
