using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Specifies the supported feedback values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailFeedbackType
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the abuse choice or state.
    /// </summary>
    Abuse = 1,
    /// <summary>
    /// Represents the authentication failure choice or state.
    /// </summary>
    AuthenticationFailure = 2,
    /// <summary>
    /// Represents the fraud choice or state.
    /// </summary>
    Fraud = 3,
    /// <summary>
    /// Represents the not spam choice or state.
    /// </summary>
    NotSpam = 4,
    /// <summary>
    /// Represents the other choice or state.
    /// </summary>
    Other = 5,
    /// <summary>
    /// Represents the virus choice or state.
    /// </summary>
    Virus = 6,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 7
}
