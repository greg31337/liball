using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported signature header values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSignatureHeaderKind
{
    /// <summary>
    /// Represents the DKIM signature choice or state.
    /// </summary>
    DkimSignature = 0,
    /// <summary>
    /// Represents the ARC message signature choice or state.
    /// </summary>
    ArcMessageSignature = 1,
    /// <summary>
    /// Represents the ARC seal choice or state.
    /// </summary>
    ArcSeal = 2,
    /// <summary>
    /// Represents the ARC authentication results choice or state.
    /// </summary>
    ArcAuthenticationResults = 3
}
