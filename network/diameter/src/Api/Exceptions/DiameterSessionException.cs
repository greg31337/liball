using System;

namespace diameter;

/// <summary>
/// Represents an invalid Diameter session lifecycle operation.
/// </summary>
public class DiameterSessionException : System.Exception
{
    /// <summary>Initializes a new session exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterSessionException(string message) : base(message)
    {
    }
}
