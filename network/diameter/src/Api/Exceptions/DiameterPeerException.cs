using System;

namespace diameter;

/// <summary>
/// Represents a Diameter peer configuration or state operation failure.
/// </summary>
public class DiameterPeerException : System.Exception
{
    /// <summary>Initializes a new peer exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterPeerException(string message) : base(message)
    {
    }
}
