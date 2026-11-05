using System;

namespace diameter;

/// <summary>
/// Represents a Diameter application registration or lifecycle failure.
/// </summary>
public class DiameterApplicationException : System.Exception
{
    /// <summary>Initializes a new application exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterApplicationException(string message) : base(message)
    {
    }
}
