using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported message classification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMessageClassificationConfidence
{
    /// <summary>
    /// No message classification is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the low choice or state.
    /// </summary>
    Low = 1,
    /// <summary>
    /// Represents the medium choice or state.
    /// </summary>
    Medium = 2,
    /// <summary>
    /// Represents the high choice or state.
    /// </summary>
    High = 3
}
