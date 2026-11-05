namespace pop3;

/// <summary>Processes one connection-scoped server-side SASL exchange.</summary>
public interface IPop3SaslServerSession
{
    /// <summary>Begins the exchange with an optional decoded client initial response.</summary>
    /// <param name="initialResponse">The decoded initial-response bytes.</param>
    /// <param name="hasInitialResponse">Whether the AUTH command contained an initial response.</param>
    /// <returns>The first server exchange step.</returns>
    Pop3SaslStep BeginAuthentication(byte[] initialResponse, bool hasInitialResponse);

    /// <summary>Processes one decoded client response.</summary>
    /// <param name="response">The decoded response bytes.</param>
    /// <returns>The next server exchange step.</returns>
    Pop3SaslStep ContinueAuthentication(byte[] response);

    /// <summary>Cancels and clears the current SASL exchange.</summary>
    void CancelAuthentication();

    /// <summary>Gets the final classified authentication result.</summary>
    /// <returns>The authentication result.</returns>
    Pop3AuthenticationResult GetAuthenticationResult();

    /// <summary>Applies an optional negotiated SASL security layer to the transport.</summary>
    /// <param name="stream">The current transport stream.</param>
    /// <returns>The secured stream, or the supplied stream when no layer was negotiated.</returns>
    System.IO.Stream ApplySecurityLayer(System.IO.Stream stream);
}
