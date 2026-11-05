using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported ARC set collection values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailArcSetCollectionStatus
{
    /// <summary>
    /// No ARC set collection is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the valid choice or state.
    /// </summary>
    Valid = 1,
    /// <summary>
    /// Represents the invalid choice or state.
    /// </summary>
    Invalid = 2
}
