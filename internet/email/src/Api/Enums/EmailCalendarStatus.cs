using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported calendar values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCalendarStatus
{
    /// <summary>
    /// No calendar is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the tentative choice or state.
    /// </summary>
    Tentative = 1,
    /// <summary>
    /// Represents the confirmed choice or state.
    /// </summary>
    Confirmed = 2,
    /// <summary>
    /// Represents the cancelled choice or state.
    /// </summary>
    Cancelled = 3,
    /// <summary>
    /// Represents the needs action choice or state.
    /// </summary>
    NeedsAction = 4,
    /// <summary>
    /// Represents the completed choice or state.
    /// </summary>
    Completed = 5,
    /// <summary>
    /// Represents the in process choice or state.
    /// </summary>
    InProcess = 6,
    /// <summary>
    /// Represents the draft choice or state.
    /// </summary>
    Draft = 7,
    /// <summary>
    /// Represents the final choice or state.
    /// </summary>
    Final = 8,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 9
}
