using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported calendar method values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCalendarMethod
{
    /// <summary>
    /// No calendar method is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the publish choice or state.
    /// </summary>
    Publish = 1,
    /// <summary>
    /// Represents the request choice or state.
    /// </summary>
    Request = 2,
    /// <summary>
    /// Represents the reply choice or state.
    /// </summary>
    Reply = 3,
    /// <summary>
    /// Represents the add choice or state.
    /// </summary>
    Add = 4,
    /// <summary>
    /// Represents the cancel choice or state.
    /// </summary>
    Cancel = 5,
    /// <summary>
    /// Represents the refresh choice or state.
    /// </summary>
    Refresh = 6,
    /// <summary>
    /// Represents the counter choice or state.
    /// </summary>
    Counter = 7,
    /// <summary>
    /// Represents the decline counter choice or state.
    /// </summary>
    DeclineCounter = 8,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 9
}
