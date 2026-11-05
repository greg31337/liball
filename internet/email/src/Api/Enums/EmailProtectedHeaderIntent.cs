using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported protected header intent values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailProtectedHeaderIntent
{
    /// <summary>
    /// Represents the clear choice or state.
    /// </summary>
    Clear = 0,
    /// <summary>
    /// Represents the cipher choice or state.
    /// </summary>
    Cipher = 1
}
