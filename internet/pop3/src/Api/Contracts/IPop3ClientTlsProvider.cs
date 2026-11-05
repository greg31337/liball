namespace pop3;

/// <summary>Upgrades an established client transport to a validated TLS stream.</summary>
public interface IPop3ClientTlsProvider
{
    /// <summary>Creates and authenticates a TLS stream over an established transport.</summary>
    /// <param name="stream">The established transport stream.</param>
    /// <param name="options">The client TLS identity and certificate policy.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking handshake.</param>
    /// <returns>The authenticated TLS stream.</returns>
    System.IO.Stream SecureClientStream(System.IO.Stream stream, Pop3TlsClientOptions options, System.Threading.CancellationToken cancellationToken);
}
