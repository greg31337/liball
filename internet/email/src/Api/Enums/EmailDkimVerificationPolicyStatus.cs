using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported DKIM verification policy values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDkimVerificationPolicyStatus
{
    /// <summary>
    /// Represents the not evaluated choice or state.
    /// </summary>
    NotEvaluated = 0,
    /// <summary>
    /// Represents the accepted choice or state.
    /// </summary>
    Accepted = 1,
    /// <summary>
    /// Represents the accepted with warnings choice or state.
    /// </summary>
    AcceptedWithWarnings = 2,
    /// <summary>
    /// Represents the rejected choice or state.
    /// </summary>
    Rejected = 3
}
