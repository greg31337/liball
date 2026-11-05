using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported ARC chain values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailArcChainStatus
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// No ARC chain is selected or present.
    /// </summary>
    None = 1,
    /// <summary>
    /// Represents the pass choice or state.
    /// </summary>
    Pass = 2,
    /// <summary>
    /// Represents the fail choice or state.
    /// </summary>
    Fail = 3
}
