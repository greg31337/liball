namespace pop3;

/// <summary>Provides client-side steps for a POP3 SASL exchange.</summary>
public interface IPop3SaslClient
{
    /// <summary>Gets the SASL mechanism name offered by this client.</summary>
    /// <returns>The registered or extension mechanism name.</returns>
    string GetMechanism();

    /// <summary>Begins the SASL exchange and optionally produces an initial response.</summary>
    /// <returns>The initial client exchange step.</returns>
    Pop3SaslStep BeginAuthentication();

    /// <summary>Processes one decoded server challenge.</summary>
    /// <param name="challenge">The decoded challenge bytes.</param>
    /// <returns>The next client exchange step.</returns>
    Pop3SaslStep ContinueAuthentication(byte[] challenge);

    /// <summary>Cancels and clears the current SASL exchange.</summary>
    void CancelAuthentication();

    /// <summary>Applies an optional negotiated SASL security layer to the transport.</summary>
    /// <param name="stream">The current transport stream.</param>
    /// <returns>The secured stream, or the supplied stream when no layer was negotiated.</returns>
    System.IO.Stream ApplySecurityLayer(System.IO.Stream stream);
}
