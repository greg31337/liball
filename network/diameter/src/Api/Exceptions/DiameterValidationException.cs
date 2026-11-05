using System;

namespace diameter;

/// <summary>
/// Represents a Diameter validation failure requested in throwing mode.
/// </summary>
public class DiameterValidationException : System.Exception
{
    /// <summary>Initializes a new validation exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterValidationException(string message) : base(message)
    {
    }
}
