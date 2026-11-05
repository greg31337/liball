namespace pop3;

/// <summary>Upgrades an accepted server transport to an authenticated TLS stream.</summary>
public interface IPop3ServerTlsProvider
{
    /// <summary>Creates and authenticates a TLS stream over an accepted transport.</summary>
    /// <param name="stream">The accepted transport stream.</param>
    /// <param name="options">The server TLS certificate and protocol policy.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking handshake.</param>
    /// <returns>The authenticated TLS stream.</returns>
    System.IO.Stream SecureServerStream(System.IO.Stream stream, Pop3TlsServerOptions options, System.Threading.CancellationToken cancellationToken);
}
