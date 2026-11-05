using System;

namespace diameter;

/// <summary>
/// Represents an invalid or conflicting Diameter dictionary definition.
/// </summary>
public class DiameterDictionaryException : System.Exception
{
    /// <summary>Initializes a new dictionary exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterDictionaryException(string message) : base(message)
    {
    }
}
