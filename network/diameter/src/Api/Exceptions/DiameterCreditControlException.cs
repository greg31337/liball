using System;

namespace diameter;

/// <summary>
/// Represents an invalid Diameter credit-control lifecycle operation.
/// </summary>
public class DiameterCreditControlException : System.Exception
{
    /// <summary>Initializes a new credit-control exception.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public DiameterCreditControlException(string message) : base(message)
    {
    }
}
