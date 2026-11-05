using System;

namespace diameter;

/// <summary>
/// Represents a Diameter route failure requested in throwing mode.
/// </summary>
public class DiameterRoutingException : System.Exception
{
    /// <summary>Initializes a new routing exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterRoutingException(string message) : base(message)
    {
    }
}
