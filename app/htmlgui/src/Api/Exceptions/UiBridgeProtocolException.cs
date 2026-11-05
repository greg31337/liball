namespace htmlgui;

/// <summary>Reports a malformed, invalid, or out-of-sequence bridge message.</summary>
public class UiBridgeProtocolException : System.Exception
{
    /// <summary>The stable protocol error code.</summary>
    public string Code;
    /// <summary>The bridge session identifier.</summary>
    public string Session;
    /// <summary>The message sequence number.</summary>
    public int Sequence;

    /// <summary>Creates a bridge protocol exception with code, session, and sequence context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="code">The stable protocol error code.</param>
    /// <param name="session">The bridge session identifier.</param>
    /// <param name="sequence">The non-negative message sequence.</param>
    public UiBridgeProtocolException(string message, string code, string session, int sequence)
        : base($"{Guard.ArgumentNotWhiteSpace(message, "message")} (protocol code '{Guard.ArgumentNotWhiteSpace(code, "code")}', session '{Guard.ArgumentNotWhiteSpace(session, "session")}', sequence '{InvariantFormatter.FormatInt32(Guard.ArgumentNonNegative(sequence, "sequence"))}').")
    {
        Code = code;
        Session = session;
        Sequence = sequence;
    }
}
