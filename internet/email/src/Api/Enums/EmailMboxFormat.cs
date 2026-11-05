using System;

namespace email;
/// <summary>
/// Specifies the supported mbox values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMboxFormat
{
    /// <summary>
    /// Represents the mboxo choice or state.
    /// </summary>
    Mboxo = 0,
    /// <summary>
    /// Represents the mboxrd choice or state.
    /// </summary>
    Mboxrd = 1
}
