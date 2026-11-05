using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported DKIM canonicalization values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDkimCanonicalizationKind
{
    /// <summary>
    /// Represents the simple choice or state.
    /// </summary>
    Simple = 0,
    /// <summary>
    /// Represents the relaxed choice or state.
    /// </summary>
    Relaxed = 1,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 2
}
