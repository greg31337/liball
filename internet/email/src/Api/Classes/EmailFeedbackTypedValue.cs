using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Represents a feedback typed value.
/// </summary>
public class EmailFeedbackTypedValue : IEmailFeedbackTypedValueContract
{
    string type;
    string value;
    /// <summary>
    /// Creates a new feedback typed value.
    /// </summary>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public EmailFeedbackTypedValue(string type, string value)
    {
        if (!EmailFeedbackReportValueParser.IsToken(type))
            throw new ArgumentException("A feedback typed-value type must be a token.", "type");
        if (value == null || value.Trim().Length == 0)
            throw new ArgumentException("A feedback typed-value value is required.", "value");
        this.type = type.ToLowerInvariant();
        this.value = value.Trim();
    }

    /// <summary>
    /// Gets the type name.
    /// </summary>
    /// <returns>The current type name.</returns>
    public string GetTypeName()
    {
        return type;
    }

    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        return value;
    }
}
