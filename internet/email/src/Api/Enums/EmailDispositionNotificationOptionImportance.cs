using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported disposition notification option importance values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDispositionNotificationOptionImportance
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the required choice or state.
    /// </summary>
    Required = 1,
    /// <summary>
    /// Represents the optional choice or state.
    /// </summary>
    Optional = 2
}
