using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported ARC verification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailArcVerificationStatus
{
    /// <summary>
    /// No ARC verification is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the pass choice or state.
    /// </summary>
    Pass = 1,
    /// <summary>
    /// Represents the fail choice or state.
    /// </summary>
    Fail = 2
}
