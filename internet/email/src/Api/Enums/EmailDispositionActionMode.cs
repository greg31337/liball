using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported disposition action values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDispositionActionMode
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the manual choice or state.
    /// </summary>
    Manual = 1,
    /// <summary>
    /// Represents the automatic choice or state.
    /// </summary>
    Automatic = 2
}
