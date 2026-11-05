using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported disposition notification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDispositionNotificationType
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the displayed choice or state.
    /// </summary>
    Displayed = 1,
    /// <summary>
    /// Represents the deleted choice or state.
    /// </summary>
    Deleted = 2,
    /// <summary>
    /// Represents the dispatched choice or state.
    /// </summary>
    Dispatched = 3,
    /// <summary>
    /// Represents the processed choice or state.
    /// </summary>
    Processed = 4,
    /// <summary>
    /// Represents the denied choice or state.
    /// </summary>
    Denied = 5,
    /// <summary>
    /// Represents the failed choice or state.
    /// </summary>
    Failed = 6
}
