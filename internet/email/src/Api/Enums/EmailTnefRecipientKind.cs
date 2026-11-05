using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported TNEF recipient values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTnefRecipientKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the to choice or state.
    /// </summary>
    To = 1,
    /// <summary>
    /// Represents the cc choice or state.
    /// </summary>
    Cc = 2,
    /// <summary>
    /// Represents the bcc choice or state.
    /// </summary>
    Bcc = 3
}
