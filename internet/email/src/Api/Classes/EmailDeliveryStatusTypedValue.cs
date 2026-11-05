using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a delivery status typed value.
/// </summary>
public class EmailDeliveryStatusTypedValue : IEmailDeliveryStatusTypedValueContract
{
    string type;
    string value;
    /// <summary>
    /// Creates a new delivery status typed value.
    /// </summary>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDeliveryStatusTypedValue(string type, string value)
    {
        if (type == null)
            throw new ArgumentNullException("type");
        if (value == null)
            throw new ArgumentNullException("value");
        if (type.Length == 0 || value.Length == 0)
            throw new ArgumentException("Typed delivery-status values require nonempty type and value components.");
        this.type = type.ToLowerInvariant();
        this.value = value;
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
