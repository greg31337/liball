using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported TNEF named property values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTnefNamedPropertyKind
{
    /// <summary>
    /// No TNEF named property is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the numeric choice or state.
    /// </summary>
    Numeric = 1,
    /// <summary>
    /// Represents the string choice or state.
    /// </summary>
    String = 2
}
