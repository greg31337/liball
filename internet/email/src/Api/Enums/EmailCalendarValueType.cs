using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported calendar value values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCalendarValueType
{
    /// <summary>
    /// Represents the date choice or state.
    /// </summary>
    Date = 0,
    /// <summary>
    /// Represents the date time choice or state.
    /// </summary>
    DateTime = 1
}
