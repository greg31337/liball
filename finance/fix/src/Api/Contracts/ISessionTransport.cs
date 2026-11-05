using System;

namespace fix;

/// <summary>Provides transport-neutral synchronous session I/O.</summary>
public interface ISessionTransport
{
    /// <summary>Opens or starts the configured transport endpoint.</summary>
    /// <param name="endpoint">The endpoint to open.</param>
    /// <param name="tls">The TLS settings.</param>
    /// <returns>The explicit transport outcome.</returns>
    TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls);
    /// <summary>Reads available bytes into caller-owned storage.</summary>
    /// <param name="destination">The destination storage.</param>
    /// <returns>The explicit read outcome.</returns>
    TransportReadResult ReadTransport(Span<byte> destination);
    /// <summary>Writes bytes from caller-owned storage.</summary>
    /// <param name="source">The source bytes.</param>
    /// <returns>The explicit write outcome.</returns>
    TransportWriteResult WriteTransport(ReadOnlySpan<byte> source);
    /// <summary>Closes the transport.</summary>
    /// <returns>The explicit close outcome.</returns>
    TransportWriteResult CloseTransport();
}
