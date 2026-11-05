using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Represents a feedback path.
/// </summary>
public class EmailFeedbackPath : IEmailFeedbackPathContract
{
    bool nullPath;
    EmailAddress? address;
    /// <summary>
    /// Creates a new feedback path.
    /// </summary>
    /// <param name = "nullPath">Whether to enable the null path behavior.</param>
    /// <param name = "address">The address used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public EmailFeedbackPath(bool nullPath, EmailAddress? address)
    {
        if (nullPath && address != null || !nullPath && address == null)
            throw new ArgumentException("A feedback path must be either the null reverse path or one mailbox.");
        this.nullPath = nullPath;
        this.address = address;
    }

    /// <summary>
    /// Determines whether the current value or state is null path.
    /// </summary>
    /// <returns><c>true</c> when the value or state is null path; otherwise, <c>false</c>.</returns>
    public bool IsNullPath()
    {
        return nullPath;
    }

    /// <summary>
    /// Determines whether the address is present or available.
    /// </summary>
    /// <returns><c>true</c> when the address is available; otherwise, <c>false</c>.</returns>
    public bool HasAddress()
    {
        return address != null;
    }

    /// <summary>
    /// Gets the address.
    /// </summary>
    /// <returns>The current address.</returns>
    public EmailAddress? GetAddress()
    {
        return address;
    }
}
