namespace uuid;

/// <summary>Reports a coded failure from an identifier operation that cannot return a result object.</summary>
public class IdentifierException : System.Exception
{
    /// <summary>The stable machine-readable reason for the failure.</summary>
    public IdentifierErrorCode Code;

    /// <summary>Creates a coded identifier exception.</summary>
    /// <param name="code">The stable machine-readable failure code.</param>
    /// <param name="message">The plain-English failure message.</param>
    public IdentifierException(IdentifierErrorCode code, string message) : base(message)
    {
        Code = code;
    }
}
