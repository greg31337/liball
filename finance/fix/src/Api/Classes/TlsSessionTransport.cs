using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace fix;

/// <summary>Provides synchronous TLS client or server session transport over a connected stream.</summary>
public class TlsSessionTransport : ISessionTransport
{
    internal static readonly SslProtocols AllowedProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    internal Stream ConnectedStream;
    internal SslStream SecureStream;
    internal SessionRole Role;
    internal TransportState CurrentState = TransportState.Disconnected;
    internal X509Certificate2? LocalCertificate;
    internal string AuthenticatedTargetHost = "";
    internal string ConfiguredAlpn = "";
    internal bool StreamClosed;

    /// <summary>Creates a TLS transport over a caller-supplied connected duplex stream.</summary>
    /// <param name="connectedStream">The connected stream to authenticate and protect.</param>
    /// <param name="role">Whether this side initiates or accepts the TLS handshake.</param>
    /// <exception cref="FixTransportException">Thrown when the stream is missing, is not duplex, or the role is invalid.</exception>
    public TlsSessionTransport(Stream connectedStream, SessionRole role)
    {
        if (connectedStream == null)
        {
            throw new FixTransportException("The connected TLS stream is missing.");
        }
        if (!connectedStream.CanRead || !connectedStream.CanWrite)
        {
            throw new FixTransportException("The connected TLS stream must be readable and writable.");
        }
        if (role != SessionRole.Initiator && role != SessionRole.Acceptor)
        {
            throw new FixTransportException("The TLS transport role is invalid.");
        }
        ConnectedStream = connectedStream;
        SecureStream = new SslStream(connectedStream, false);
        Role = role;
    }

    /// <summary>Authenticates the connected stream with TLS 1.2 or newer and the configured FIXS ALPN.</summary>
    /// <param name="endpoint">The endpoint supplying a fallback initiator target host.</param>
    /// <param name="tls">The TLS certificate, hostname, revocation, and ALPN settings.</param>
    /// <returns>The authenticated state or a stable configuration failure.</returns>
    public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
    {
        if (CurrentState == TransportState.Connected && SecureStream.IsAuthenticated)
        {
            return CreateWriteSuccess(0);
        }
        if (CurrentState == TransportState.Closed)
        {
            return CreateWriteFailure(CurrentState, "transport-closed", "The TLS transport is closed.");
        }
        if (tls == null || !tls.Enabled)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tls-disabled", "TLS must be enabled for a TLS transport.");
        }
        if (tls.Alpn == null || tls.Alpn.Length == 0)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "missing-tls-alpn", "The FIXS ALPN identifier is missing.");
        }
        ConfiguredAlpn = tls.Alpn;
        CurrentState = TransportState.Connecting;
        if (Role == SessionRole.Initiator)
        {
            return AuthenticateClient(endpoint, tls);
        }
        return AuthenticateServer(tls);
    }

    /// <summary>Reads one decrypted synchronous chunk from the authenticated stream.</summary>
    /// <param name="destination">The destination storage.</param>
    /// <returns>The decrypted byte count, EOF indication, and resulting state.</returns>
    public TransportReadResult ReadTransport(Span<byte> destination)
    {
        if (CurrentState != TransportState.Connected || !SecureStream.IsAuthenticated)
        {
            if (CurrentState == TransportState.Closed) return CreateReadFailure(CurrentState, "transport-closed", "The TLS transport is closed.");
            if (CurrentState == TransportState.Faulted) return CreateReadFailure(CurrentState, "transport-faulted", "The TLS transport is faulted.");
            return CreateReadFailure(CurrentState, "transport-not-connected", "The TLS transport is not authenticated.");
        }
        TransportReadResult result = new TransportReadResult();
        result.State = TransportState.Connected;
        if (destination.Length == 0)
        {
            return result;
        }
        result.BytesRead = SecureStream.Read(destination);
        if (result.BytesRead == 0)
        {
            CloseSecureStream();
            CurrentState = TransportState.Closed;
            result.State = TransportState.Closed;
            result.EndOfStream = true;
        }
        return result;
    }

    /// <summary>Writes and encrypts the complete caller-supplied byte slice.</summary>
    /// <param name="source">The plaintext source bytes.</param>
    /// <returns>The consumed byte count and resulting state.</returns>
    public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
    {
        if (CurrentState != TransportState.Connected || !SecureStream.IsAuthenticated)
        {
            if (CurrentState == TransportState.Closed) return CreateWriteFailure(CurrentState, "transport-closed", "The TLS transport is closed.");
            if (CurrentState == TransportState.Faulted) return CreateWriteFailure(CurrentState, "transport-faulted", "The TLS transport is faulted.");
            return CreateWriteFailure(CurrentState, "transport-not-connected", "The TLS transport is not authenticated.");
        }
        if (source.Length != 0)
        {
            SecureStream.Write(source);
        }
        return CreateWriteSuccess(source.Length);
    }

    /// <summary>Closes the TLS session and its caller-supplied underlying stream.</summary>
    /// <returns>The closed transport outcome.</returns>
    public TransportWriteResult CloseTransport()
    {
        CloseSecureStream();
        CurrentState = TransportState.Closed;
        TransportWriteResult result = new TransportWriteResult();
        result.State = TransportState.Closed;
        return result;
    }

    internal TransportWriteResult AuthenticateClient(SessionEndpoint endpoint, SessionTlsSettings tls)
    {
        string targetHost = tls.TargetHost;
        if ((targetHost == null || targetHost.Length == 0) && endpoint != null)
        {
            targetHost = endpoint.Host;
        }
        if (targetHost == null || targetHost.Length == 0)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "missing-tls-target-host", "The TLS server target host is missing.");
        }
        AuthenticatedTargetHost = targetHost;
        SslClientAuthenticationOptions options = new SslClientAuthenticationOptions();
        options.TargetHost = targetHost;
        options.EnabledSslProtocols = AllowedProtocols;
        options.CertificateRevocationCheckMode = ResolveRevocationMode(tls.CheckCertificateRevocation);
        options.ApplicationProtocols = CreateApplicationProtocols(tls.Alpn);
        options.EncryptionPolicy = EncryptionPolicy.RequireEncryption;
        if (!tls.RequirePeerCertificate)
        {
            options.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }
        if (tls.CertificatePath != null && tls.CertificatePath.Length != 0)
        {
            TransportWriteResult? certificateFailure = LoadLocalCertificate(tls);
            if (certificateFailure != null)
            {
                return certificateFailure;
            }
            X509Certificate2? localCertificate = LocalCertificate;
            if (localCertificate == null)
            {
                CurrentState = TransportState.Faulted;
                return CreateWriteFailure(CurrentState, "missing-tls-certificate", "The local TLS certificate could not be loaded.");
            }
            X509CertificateCollection certificates = new X509CertificateCollection();
            certificates.Add(localCertificate);
            options.ClientCertificates = certificates;
        }
        Task authentication = SecureStream.AuthenticateAsClientAsync(options);
        return CompleteAuthenticationTask(authentication);
    }

    internal TransportWriteResult AuthenticateServer(SessionTlsSettings tls)
    {
        TransportWriteResult? certificateFailure = LoadLocalCertificate(tls);
        if (certificateFailure != null)
        {
            return certificateFailure;
        }
        SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();
        options.ServerCertificate = LocalCertificate;
        options.ClientCertificateRequired = tls.RequirePeerCertificate;
        options.EnabledSslProtocols = AllowedProtocols;
        options.CertificateRevocationCheckMode = ResolveRevocationMode(tls.CheckCertificateRevocation);
        options.ApplicationProtocols = CreateApplicationProtocols(tls.Alpn);
        options.EncryptionPolicy = EncryptionPolicy.RequireEncryption;
        Task authentication = SecureStream.AuthenticateAsServerAsync(options);
        return CompleteAuthenticationTask(authentication);
    }

    internal TransportWriteResult? LoadLocalCertificate(SessionTlsSettings tls)
    {
        if (tls.CertificatePath == null || tls.CertificatePath.Length == 0)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "missing-tls-certificate", "The local TLS certificate path is missing.");
        }
        if (!File.Exists(tls.CertificatePath))
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tls-certificate-not-found", "The local TLS certificate file does not exist.");
        }
        LocalCertificate = X509CertificateLoader.LoadPkcs12FromFile(tls.CertificatePath, tls.CertificatePassword, X509KeyStorageFlags.DefaultKeySet);
        return null;
    }

    internal TransportWriteResult CompleteAuthentication()
    {
        if (!SecureStream.IsAuthenticated || SecureStream.NegotiatedApplicationProtocol.ToString() != ConfiguredAlpn)
        {
            CloseSecureStream();
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tls-alpn-not-negotiated", "The peer did not negotiate the configured FIXS ALPN identifier.");
        }
        if (SecureStream.SslProtocol != SslProtocols.Tls12 && SecureStream.SslProtocol != SslProtocols.Tls13)
        {
            CloseSecureStream();
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tls-protocol-below-minimum", "The negotiated TLS protocol is below TLS 1.2.");
        }
        CurrentState = TransportState.Connected;
        return CreateWriteSuccess(0);
    }

    internal TransportWriteResult CompleteAuthenticationTask(Task authentication)
    {
        while (!authentication.IsCompleted)
        {
            Thread.Sleep(1);
        }
        if (!authentication.IsCompletedSuccessfully)
        {
            string text = authentication.Exception == null ? "TLS authentication failed." : authentication.Exception.GetBaseException().Message;
            CloseSecureStream();
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tls-authentication-failed", text);
        }
        return CompleteAuthentication();
    }

    internal static List<SslApplicationProtocol> CreateApplicationProtocols(string alpn)
    {
        List<SslApplicationProtocol> protocols = [];
        protocols.Add(new SslApplicationProtocol(alpn));
        return protocols;
    }

    internal static X509RevocationMode ResolveRevocationMode(bool checkRevocation)
    {
        if (checkRevocation)
        {
            return X509RevocationMode.Online;
        }
        return X509RevocationMode.NoCheck;
    }

    internal void CloseSecureStream()
    {
        if (!StreamClosed)
        {
            SecureStream.Close();
            StreamClosed = true;
        }
        X509Certificate2? certificate = LocalCertificate;
        LocalCertificate = null;
        if (certificate != null)
        {
            certificate.Dispose();
        }
    }

    internal TransportWriteResult CreateWriteSuccess(int bytesWritten)
    {
        TransportWriteResult result = new TransportWriteResult();
        result.State = CurrentState;
        result.BytesWritten = bytesWritten;
        return result;
    }

    internal static TransportWriteResult CreateWriteFailure(TransportState state, string code, string message)
    {
        TransportWriteResult result = new TransportWriteResult();
        result.State = state;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static TransportReadResult CreateReadFailure(TransportState state, string code, string message)
    {
        TransportReadResult result = new TransportReadResult();
        result.State = state;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
