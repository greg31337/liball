using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported TNEF attribute level values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTnefAttributeLevel
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the message choice or state.
    /// </summary>
    Message = 1,
    /// <summary>
    /// Represents the attachment choice or state.
    /// </summary>
    Attachment = 2
}
