using System;
using System.IO;
using System.Net.Sockets;

namespace ftp;

internal class FtpProtectedDataStream
{
    internal FtpClearDataStream Transfer;
    internal FtpProtectedDataStreamRequest? Request;
    internal FtpTlsClientStream? ClientTls;
    internal FtpTlsServerStream? ServerTls;
    internal FtpTlsStreamResult? TlsResult;
    internal NetworkStream? TransportStream;
    internal bool ClientRoleAuthenticated;
    internal bool ServerRoleAuthenticated;
    internal FtpTlsSessionReuseEvidence? SessionReuseEvidence;
    internal bool Started;

    internal FtpProtectedDataStream(FtpDataConnectionCoordinator? coordinator)
    {
        Transfer = new FtpClearDataStream(coordinator);
        Request = null;
        ClientTls = null;
        ServerTls = null;
        TlsResult = null;
        TransportStream = null;
        ClientRoleAuthenticated = false;
        ServerRoleAuthenticated = false;
        SessionReuseEvidence = null;
        Started = false;
    }

    internal FtpReply BeginSend(FtpProtectedDataStreamRequest? request)
    {
        BeginProtectedTransfer(request, true);
        return Transfer.Result.PreliminaryReply;
    }

    internal FtpReply BeginReceive(FtpProtectedDataStreamRequest? request)
    {
        BeginProtectedTransfer(request, false);
        return Transfer.Result.PreliminaryReply;
    }

    internal FtpTransferPumpResult ExecuteSendData()
    {
        return Transfer.ExecuteSendData();
    }

    internal FtpTransferPumpResult ExecuteReceiveData()
    {
        return Transfer.ExecuteReceiveData();
    }

    internal FtpReply CreateFinalReply()
    {
        return Transfer.CreateFinalReply();
    }

    internal void ApplyFinalReply(FtpReply? reply)
    {
        Transfer.ApplyFinalReply(reply);
    }

    internal void BeginProtectedTransfer(
        FtpProtectedDataStreamRequest? request,
        bool sending)
    {
        if (Started)
        {
            throw new FtpProtocolException(
                "A protected FTP data-stream operation already started.", null);
        }

        ValidateRequest(request, sending);
        if (request == null || request.Transfer == null)
        {
            throw new FtpConfigurationException(
                "A protected FTP data-stream request is required.", null);
        }

        Request = request;
        request.Transfer.DataStreamFactory = socket => CreateProtectedStream(socket);
        request.Transfer.DataStreamShutdown = () => ShutdownProtectedStream();
        Started = true;
        if (sending)
        {
            Transfer.BeginSend(request.Transfer);
        }
        else
        {
            Transfer.BeginReceive(request.Transfer);
        }
    }

    internal Stream CreateProtectedStream(Socket socket)
    {
        FtpProtectedDataStreamRequest request = RequireRequest();
        if (request.TlsOptions == null || request.Transfer == null ||
            request.Transfer.Options == null)
        {
            throw new FtpConfigurationException(
                "Complete protected data-stream settings are required.", null);
        }

        TransportStream = new NetworkStream(socket, false);
        if (request.Role == FtpDataChannelRole.Client)
        {
            FtpTlsClientStreamRequest clientRequest = new FtpTlsClientStreamRequest();
            clientRequest.TransportStream = TransportStream;
            clientRequest.Options = request.TlsOptions;
            clientRequest.TargetHost = request.TargetHost;
            clientRequest.HandshakeTimeoutMilliseconds =
                request.HandshakeTimeoutMilliseconds;
            clientRequest.CancellationToken =
                request.Transfer.Options.Timeouts.CancellationToken;
            clientRequest.LeaveTransportOpen = false;
            clientRequest.AllowTlsResume = true;
            clientRequest.CertificateValidation = request.CertificateValidation;
            clientRequest.RemoteEndpoint = request.RemoteEndpoint;
            clientRequest.CertificateValidator = request.CertificateValidator;
            if (request.ClientAuthentication == null)
            {
                ClientTls = new FtpTlsClientStream(clientRequest);
                TlsResult = ClientTls.AuthenticateDataClient();
            }
            else
            {
                Stream clientStream = request.ClientAuthentication.Invoke(clientRequest);
                ClientRoleAuthenticated = true;
                TransportStream = null;
                ApplySessionReusePolicy(request, true);
                return RequireAuthenticatedStream(clientStream);
            }
            ClientRoleAuthenticated = true;
        }
        else
        {
            FtpTlsServerStreamRequest serverRequest = new FtpTlsServerStreamRequest();
            serverRequest.TransportStream = TransportStream;
            serverRequest.Options = request.TlsOptions;
            serverRequest.HandshakeTimeoutMilliseconds =
                request.HandshakeTimeoutMilliseconds;
            serverRequest.CancellationToken =
                request.Transfer.Options.Timeouts.CancellationToken;
            serverRequest.LeaveTransportOpen = false;
            serverRequest.AllowTlsResume = true;
            serverRequest.CertificateValidation = request.CertificateValidation;
            serverRequest.CertificateSelection = request.CertificateSelection;
            serverRequest.CertificateProvider = request.CertificateProvider;
            serverRequest.HostName = request.HostName;
            serverRequest.LocalEndpoint = request.LocalEndpoint;
            serverRequest.RemoteEndpoint = request.RemoteEndpoint;
            if (request.ServerAuthentication == null)
            {
                ServerTls = new FtpTlsServerStream(serverRequest);
                TlsResult = ServerTls.AuthenticateDataServer();
            }
            else
            {
                Stream serverStream = request.ServerAuthentication.Invoke(serverRequest);
                ServerRoleAuthenticated = true;
                TransportStream = null;
                ApplySessionReusePolicy(request, true);
                return RequireAuthenticatedStream(serverStream);
            }
            ServerRoleAuthenticated = true;
        }

        TransportStream = null;
        if (TlsResult == null || TlsResult.Stream == null)
        {
            throw new FtpTlsException(
                "The protected FTP data connection has no TLS stream.", null);
        }
        ApplySessionReusePolicy(request, TlsResult.TlsResumeAllowed);
        return TlsResult.Stream;
    }

    internal void ShutdownProtectedStream()
    {
        FtpProtectedDataStreamRequest request = RequireRequest();
        FtpTlsStreamResult? result = TlsResult;
        if (result == null || result.Stream == null ||
            request.Transfer == null || request.Transfer.Options == null)
        {
            return;
        }

        System.Threading.Tasks.Task shutdown =
            result.Stream.ShutdownAsync();
        bool completed = shutdown.Wait(
            request.HandshakeTimeoutMilliseconds,
            request.Transfer.Options.Timeouts.CancellationToken);
        if (!completed)
        {
            throw new TimeoutException(
                "The protected FTP data TLS shutdown timed out.");
        }
        shutdown.GetAwaiter().GetResult();
    }

    internal void ApplySessionReusePolicy(
        FtpProtectedDataStreamRequest request,
        bool tlsResumeAllowed)
    {
        FtpTlsSessionReuseEvidence? evidence = request.SessionReuseEvidence;
        if (evidence == null && TlsResult != null && TlsResult.Stream != null)
        {
            evidence = FtpTlsSessionReusePolicy.InspectRuntime(TlsResult.Stream);
        }

        SessionReuseEvidence = evidence;
        if (TlsResult != null)
        {
            TlsResult.SessionReuseEvidence = evidence;
            FtpTlsDiagnosticsFactory.ApplySessionReuse(
                TlsResult.Diagnostics, evidence);
        }
        if (request.TlsOptions == null)
        {
            throw new FtpConfigurationException(
                "TLS options are required for session-reuse policy.", null);
        }
        FtpTlsSessionReusePolicy.RequireSatisfied(
            request.TlsOptions.RequireDataSessionReuse,
            tlsResumeAllowed,
            evidence);
    }

    internal Stream RequireAuthenticatedStream(Stream? stream)
    {
        if (stream == null || !stream.CanRead || !stream.CanWrite)
        {
            throw new FtpTlsException(
                "The protected FTP data authentication returned an invalid stream.", null);
        }
        return stream;
    }

    internal FtpProtectedDataStreamRequest RequireRequest()
    {
        if (Request == null)
        {
            throw new FtpProtocolException(
                "A protected FTP data-stream request is unavailable.", null);
        }
        return Request;
    }

    internal static void ValidateRequest(
        FtpProtectedDataStreamRequest? request,
        bool sending)
    {
        if (request == null || request.Transfer == null || request.TlsOptions == null)
        {
            throw new FtpConfigurationException(
                "Transfer and TLS settings are required for a protected data stream.", null);
        }
        if (request.Role != FtpDataChannelRole.Client &&
            request.Role != FtpDataChannelRole.Server)
        {
            throw new FtpConfigurationException(
                "The protected data-channel FTP role is invalid.", null);
        }
        if (request.TlsOptions.DataProtectionLevel != FtpDataProtectionLevel.Private)
        {
            throw new FtpConfigurationException(
                "A protected FTP data stream requires PROT P.", null);
        }
        if (request.Transfer.DataStreamFactory != null)
        {
            throw new FtpConfigurationException(
                "A protected data stream owns its transport stream factory.", null);
        }
        if (request.Transfer.DataStreamShutdown != null)
        {
            throw new FtpConfigurationException(
                "A protected data stream owns its TLS shutdown callback.", null);
        }

        FtpConfigurationValidator.ValidateTlsOptions(request.TlsOptions);
        FtpClearDataStream.ValidateRequest(request.Transfer, sending);
        if (request.HandshakeTimeoutMilliseconds <= 0 &&
            request.HandshakeTimeoutMilliseconds != System.Threading.Timeout.Infinite)
        {
            throw new FtpConfigurationException(
                "A protected data-channel TLS timeout must be positive.", null);
        }
        if (request.Role == FtpDataChannelRole.Client &&
            request.RemoteEndpoint == null &&
            (string.IsNullOrWhiteSpace(request.TargetHost) ||
                FtpConfigurationValidator.HasInvalidText(request.TargetHost)))
        {
            throw new FtpConfigurationException(
                "The FTP TLS client role requires a certificate target host.", null);
        }
        if (request.Role == FtpDataChannelRole.Server &&
            request.TlsOptions.ServerCertificate == null &&
            request.CertificateSelection == null && request.CertificateProvider == null &&
            request.ServerAuthentication == null)
        {
            throw new FtpConfigurationException(
                "The FTP TLS server role requires certificate selection.", null);
        }
    }
}
