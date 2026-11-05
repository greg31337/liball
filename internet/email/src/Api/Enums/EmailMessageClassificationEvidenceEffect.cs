using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported message classification evidence values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMessageClassificationEvidenceEffect
{
    /// <summary>
    /// Represents the supports choice or state.
    /// </summary>
    Supports = 0,
    /// <summary>
    /// Represents the contradicts choice or state.
    /// </summary>
    Contradicts = 1
}
