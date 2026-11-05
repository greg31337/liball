using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported ARC verification failure values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailArcVerificationFailureKind
{
    /// <summary>
    /// No ARC verification failure is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the structure choice or state.
    /// </summary>
    Structure = 1,
    /// <summary>
    /// Represents the message signature choice or state.
    /// </summary>
    MessageSignature = 2,
    /// <summary>
    /// Represents the seal choice or state.
    /// </summary>
    Seal = 3
}
