using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported message classification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMessageClassificationKind
{
    /// <summary>
    /// Represents the automated choice or state.
    /// </summary>
    Automated = 0,
    /// <summary>
    /// Represents the mailing list choice or state.
    /// </summary>
    MailingList = 1,
    /// <summary>
    /// Represents the bounce choice or state.
    /// </summary>
    Bounce = 2,
    /// <summary>
    /// Represents the receipt choice or state.
    /// </summary>
    Receipt = 3
}
