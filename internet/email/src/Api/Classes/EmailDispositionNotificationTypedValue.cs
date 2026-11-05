using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a disposition notification typed value.
/// </summary>
public class EmailDispositionNotificationTypedValue : IEmailDispositionNotificationTypedValueContract
{
    string type;
    string value;
    /// <summary>
    /// Creates a new disposition notification typed value.
    /// </summary>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDispositionNotificationTypedValue(string type, string value)
    {
        if (type == null)
            throw new ArgumentNullException("type");
        if (value == null)
            throw new ArgumentNullException("value");
        if (!EmailDispositionNotificationValueParser.IsToken(type) || value.Trim().Length == 0)
            throw new ArgumentException("Typed MDN values require a token type and nonempty value.");
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
