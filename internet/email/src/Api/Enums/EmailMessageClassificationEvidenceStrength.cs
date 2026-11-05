using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported message classification evidence values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMessageClassificationEvidenceStrength
{
    /// <summary>
    /// Represents the weak choice or state.
    /// </summary>
    Weak = 0,
    /// <summary>
    /// Represents the moderate choice or state.
    /// </summary>
    Moderate = 1,
    /// <summary>
    /// Represents the strong choice or state.
    /// </summary>
    Strong = 2
}
