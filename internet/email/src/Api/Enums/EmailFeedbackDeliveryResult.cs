using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Specifies the supported feedback delivery result values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailFeedbackDeliveryResult
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the delivered choice or state.
    /// </summary>
    Delivered = 1,
    /// <summary>
    /// Represents the spam choice or state.
    /// </summary>
    Spam = 2,
    /// <summary>
    /// Represents the policy choice or state.
    /// </summary>
    Policy = 3,
    /// <summary>
    /// Represents the reject choice or state.
    /// </summary>
    Reject = 4,
    /// <summary>
    /// Represents the other choice or state.
    /// </summary>
    Other = 5,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 6
}
