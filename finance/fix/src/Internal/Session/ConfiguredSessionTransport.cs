using System;
using System.IO;
using System.Net.Sockets;

namespace fix;

internal class ConfiguredSessionTransport : ISessionTransport
{
    internal SessionRole Role;
    internal bool UseTls;
    internal TcpInitiatorTransport? Initiator;
    internal TcpAcceptorTransport? Acceptor;
    internal TlsSessionTransport? Secure;

    internal ConfiguredSessionTransport(SessionSettings settings)
    {
        if (settings == null)
        {
            throw new FixTransportException("Configured transport settings are missing.");
        }
        if (settings.Role != SessionRole.Initiator && settings.Role != SessionRole.Acceptor)
        {
            throw new FixTransportException("The configured transport role is invalid.");
        }
        Role = settings.Role;
        UseTls = settings.Tls != null && settings.Tls.Enabled;
        if (Role == SessionRole.Initiator)
        {
            Initiator = new TcpInitiatorTransport(settings);
        }
        else
        {
            Acceptor = new TcpAcceptorTransport(settings);
        }
    }

    /// <summary>Opens the configured TCP transport and optional TLS layer.</summary>
    /// <param name="endpoint">The configured transport endpoint.</param>
    /// <param name="tls">The optional TLS settings.</param>
    /// <returns>The explicit transport outcome.</returns>
    public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
    {
        TlsSessionTransport? existingSecure = Secure;
        if (existingSecure != null)
        {
            return existingSecure.OpenTransport(endpoint, tls);
        }
        TransportWriteResult opened;
        if (Role == SessionRole.Initiator)
        {
            TcpInitiatorTransport? initiator = Initiator;
            if (initiator == null) return CreateConfiguredFailure("missing-initiator-transport", "The configured initiator transport is missing.");
            opened = initiator.OpenTransport(endpoint, tls);
        }
        else
        {
            TcpAcceptorTransport? acceptor = Acceptor;
            if (acceptor == null) return CreateConfiguredFailure("missing-acceptor-transport", "The configured acceptor transport is missing.");
            opened = acceptor.OpenTransport(endpoint, tls);
        }
        if (!UseTls || opened.State != TransportState.Connected || opened.ErrorCode.Length != 0)
        {
            return opened;
        }
        Socket? socket = Role == SessionRole.Initiator ? Initiator == null ? null : Initiator.CurrentSocket : Acceptor == null ? null : Acceptor.CurrentSocket;
        if (socket == null)
        {
            return CreateConfiguredFailure("missing-connected-socket", "The connected TCP socket is unavailable for TLS.");
        }
        Stream stream = new NetworkStream(socket, false);
        Secure = new TlsSessionTransport(stream, Role);
        return Secure.OpenTransport(endpoint, tls);
    }

    /// <summary>Reads one chunk from the configured cleartext or TLS transport.</summary>
    /// <param name="destination">The destination storage.</param>
    /// <returns>The explicit transport read outcome.</returns>
    public TransportReadResult ReadTransport(Span<byte> destination)
    {
        TlsSessionTransport? secure = Secure;
        if (secure != null) return secure.ReadTransport(destination);
        if (Role == SessionRole.Initiator)
        {
            TcpInitiatorTransport? initiator = Initiator;
            if (initiator != null) return initiator.ReadTransport(destination);
        }
        else
        {
            TcpAcceptorTransport? acceptor = Acceptor;
            if (acceptor != null) return acceptor.ReadTransport(destination);
        }
        TransportReadResult missing = new TransportReadResult();
        missing.State = TransportState.Faulted;
        missing.ErrorCode = "missing-configured-transport";
        missing.ErrorMessage = "The configured transport is missing.";
        return missing;
    }

    /// <summary>Writes one chunk to the configured cleartext or TLS transport.</summary>
    /// <param name="source">The source bytes.</param>
    /// <returns>The explicit transport write outcome.</returns>
    public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
    {
        TlsSessionTransport? secure = Secure;
        if (secure != null) return secure.WriteTransport(source);
        if (Role == SessionRole.Initiator)
        {
            TcpInitiatorTransport? initiator = Initiator;
            if (initiator != null) return initiator.WriteTransport(source);
        }
        else
        {
            TcpAcceptorTransport? acceptor = Acceptor;
            if (acceptor != null) return acceptor.WriteTransport(source);
        }
        return CreateConfiguredFailure("missing-configured-transport", "The configured transport is missing.");
    }

    /// <summary>Closes the optional TLS layer and underlying TCP transport.</summary>
    /// <returns>The explicit closed transport outcome.</returns>
    public TransportWriteResult CloseTransport()
    {
        TlsSessionTransport? secure = Secure;
        Secure = null;
        if (secure != null) secure.CloseTransport();
        if (Role == SessionRole.Initiator)
        {
            TcpInitiatorTransport? initiator = Initiator;
            if (initiator != null) return initiator.CloseTransport();
        }
        else
        {
            TcpAcceptorTransport? acceptor = Acceptor;
            if (acceptor != null) return acceptor.CloseTransport();
        }
        return CreateConfiguredFailure("missing-configured-transport", "The configured transport is missing.");
    }

    internal static TransportWriteResult CreateConfiguredFailure(string code, string text)
    {
        TransportWriteResult result = new TransportWriteResult();
        result.State = TransportState.Faulted;
        result.ErrorCode = code;
        result.ErrorMessage = text;
        return result;
    }
}
