using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported DKIM signing attempt values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDkimSigningAttemptStatus
{
    /// <summary>
    /// Represents the success choice or state.
    /// </summary>
    Success = 0,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 1,
    /// <summary>
    /// Represents the prohibited algorithm choice or state.
    /// </summary>
    ProhibitedAlgorithm = 2
}
