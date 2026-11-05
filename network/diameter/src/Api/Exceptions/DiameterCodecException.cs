using System;

namespace diameter;

/// <summary>
/// Represents exceptional codec misuse or an unrecoverable codec failure.
/// </summary>
public class DiameterCodecException : System.Exception
{
    /// <summary>Initializes a new codec exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterCodecException(string message) : base(message)
    {
    }
}
