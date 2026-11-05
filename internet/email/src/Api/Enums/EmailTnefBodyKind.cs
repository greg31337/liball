using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported TNEF body values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTnefBodyKind
{
    /// <summary>
    /// Represents the plain text choice or state.
    /// </summary>
    PlainText = 0,
    /// <summary>
    /// Represents the HTML choice or state.
    /// </summary>
    Html = 1,
    /// <summary>
    /// Represents the rich text choice or state.
    /// </summary>
    RichText = 2
}
