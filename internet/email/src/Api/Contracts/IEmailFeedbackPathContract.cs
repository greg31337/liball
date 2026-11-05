using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedbackPath"/>.
/// </summary>
public interface IEmailFeedbackPathContract
{
    /// <summary>
    /// Determines whether the current value or state is null path.
    /// </summary>
    /// <returns><c>true</c> when the value or state is null path; otherwise, <c>false</c>.</returns>
    bool IsNullPath();
    /// <summary>
    /// Determines whether the address is present or available.
    /// </summary>
    /// <returns><c>true</c> when the address is available; otherwise, <c>false</c>.</returns>
    bool HasAddress();
    /// <summary>
    /// Gets the address.
    /// </summary>
    /// <returns>The current address.</returns>
    EmailAddress? GetAddress();
}
