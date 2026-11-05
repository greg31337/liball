using System;

namespace diameter;

/// <summary>
/// Represents a Diameter transport creation or input/output lifecycle failure.
/// </summary>
public class DiameterTransportException : System.Exception
{
    /// <summary>Initializes a new transport exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterTransportException(string message) : base(message)
    {
    }
}
