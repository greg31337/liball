using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ftp;

internal class FtpTlsServerStream
{
    internal FtpTlsServerStreamRequest Request;
    internal SslStream? SecureStream;
    internal bool Started;
    internal bool Failed;
    internal bool Closed;
    internal FtpServerCertificatePolicy? CertificatePolicy;

    internal FtpTlsServerStream(FtpTlsServerStreamRequest? request)
    {
        ValidateRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A TLS server stream request is required.", null);
        }

        Request = request;
        SecureStream = null;
        Started = false;
        Failed = false;
        Closed = false;
        CertificatePolicy = null;
    }

    internal FtpTlsStreamResult AuthenticateImplicitServer()
    {
        return AuthenticateServer(true);
    }

    internal FtpTlsStreamResult UpgradeExplicitServer(FtpReply? authorizationReply)
    {
        FtpTlsHandshake.RequireExplicitAuthorizationReply(authorizationReply);
        return AuthenticateServer(false);
    }

    internal FtpTlsStreamResult AuthenticateDataServer()
    {
        FtpTlsStreamResult result = AuthenticateServer(false);
        result.ExplicitUpgrade = false;
        result.DataChannel = true;
        result.Diagnostics.ExplicitUpgrade = false;
        result.Diagnostics.DataChannel = true;
        return result;
    }

    internal FtpTlsStreamResult AuthenticateServer(bool implicitTls)
    {
        RequireFreshStream();
        if (Request.TransportStream == null || Request.Options == null)
        {
            throw new FtpConfigurationException(
                "A complete TLS server stream request is required.", null);
        }

        Started = true;
        SecureStream = new SslStream(Request.TransportStream, Request.LeaveTransportOpen);
        SslServerAuthenticationOptions authentication =
            new SslServerAuthenticationOptions();
        if (Request.CertificateProvider == null)
        {
            authentication = CreateAuthenticationOptions();
        }
        else
        {
            CertificatePolicy = new FtpServerCertificatePolicy(
                Request.CertificateProvider,
                Request.HostName,
                Request.LocalEndpoint,
                Request.RemoteEndpoint,
                implicitTls,
                Request.Options,
                Request.CertificateValidation);
        }
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            if (CertificatePolicy == null)
            {
                FtpTlsHandshake.AuthenticateServer(
                    SecureStream,
                    authentication,
                    Request.HandshakeTimeoutMilliseconds,
                    Request.CancellationToken);
            }
            else
            {
                FtpTlsHandshake.AuthenticateSelectedServer(
                    SecureStream,
                    CertificatePolicy.SelectServerOptions,
                    CertificatePolicy,
                    Request.HandshakeTimeoutMilliseconds,
                    Request.CancellationToken);
            }
            return null;
        });
        if (!operation.Succeeded)
        {
            if (FtpCancellation.IsCancellationRequested(Request.CancellationToken))
            {
                FailAndThrow(new OperationCanceledException(
                    "The FTP TLS server handshake was canceled.",
                    operation.Failure,
                    Request.CancellationToken));
            }
            if (operation.Canceled)
            {
                FailAndThrow(new TimeoutException(
                    "The FTP TLS server handshake timed out.", operation.Failure));
            }
            if (CertificatePolicy != null && CertificatePolicy.FailureMessage != null)
            {
                FailAndThrow(new FtpTlsException(
                    CertificatePolicy.FailureMessage, operation.Failure));
            }
            FailAndThrow(operation.Failure);
        }
        SslStream? completedStream = SecureStream;
        if (completedStream == null)
        {
            FailAndThrow(new FtpTlsException(
                "The FTP TLS server handshake completed without a secure stream.", null));
            throw new FtpTlsException(
                "The FTP TLS server handshake completed without a secure stream.", null);
        }

        return FtpTlsHandshake.CreateResult(
            completedStream, true, implicitTls, Request.AllowTlsResume);
    }

    internal SslServerAuthenticationOptions CreateAuthenticationOptions()
    {
        if (Request.Options == null)
        {
            throw new FtpConfigurationException("TLS server options are required.", null);
        }

        SslServerAuthenticationOptions authentication = new SslServerAuthenticationOptions();
        authentication.ServerCertificate = Request.Options.ServerCertificate;
        authentication.ServerCertificateSelectionCallback = Request.CertificateSelection;
        authentication.ClientCertificateRequired = Request.Options.RequireClientCertificate;
        authentication.EnabledSslProtocols = Request.Options.Protocols;
        authentication.AllowTlsResume = Request.AllowTlsResume;
        authentication.CertificateRevocationCheckMode =
            Request.Options.CheckCertificateRevocation ?
                X509RevocationMode.Online : X509RevocationMode.NoCheck;
        authentication.RemoteCertificateValidationCallback = Request.CertificateValidation;
        return authentication;
    }

    internal void CloseServerTls()
    {
        if (Closed)
        {
            return;
        }

        Closed = true;
        FtpFaultIsolationResult cleanup = DisposeSecureStream();
        if (!cleanup.Succeeded)
        {
            throw new FtpTlsException(
                "The FTP TLS server stream could not be closed.", cleanup.Failure);
        }
    }

    internal void ShutdownServerTlsForClearControl()
    {
        if (Closed)
        {
            throw new FtpProtocolException(
                "The FTP TLS server stream is already closed.", null);
        }
        if (!Request.LeaveTransportOpen)
        {
            throw new FtpConfigurationException(
                "CCC requires the server TLS stream to leave the control transport open.", null);
        }

        Closed = true;
        FtpFaultIsolationResult shutdown = ShutdownSecureStream();
        FtpFaultIsolationResult cleanup = DisposeSecureStream();
        if (!shutdown.Succeeded)
        {
            Failed = true;
            throw new FtpTlsException(
                "The FTP TLS server could not send an orderly CCC shutdown.",
                shutdown.Failure);
        }
        if (!cleanup.Succeeded)
        {
            Failed = true;
            throw new FtpTlsException(
                "The FTP TLS server stream could not be released after CCC.",
                cleanup.Failure);
        }
    }

    internal void FailAndThrow(Exception? failure)
    {
        Failed = true;
        Closed = true;
        FtpFaultIsolationResult cleanup = DisposeSecureStream();
        if (failure == null && !cleanup.Succeeded)
        {
            failure = cleanup.Failure;
        }
        OperationCanceledException? canceled = failure as OperationCanceledException;
        if (canceled != null)
        {
            throw canceled;
        }
        TimeoutException? timedOut = failure as TimeoutException;
        if (timedOut != null)
        {
            throw timedOut;
        }
        FtpTlsException? tlsFailure = failure as FtpTlsException;
        if (tlsFailure != null)
        {
            throw tlsFailure;
        }
        throw new FtpTlsException("The FTP TLS server handshake failed.", failure);
    }

    internal FtpFaultIsolationResult DisposeSecureStream()
    {
        FtpResourceLifetime lifetime = new FtpResourceLifetime(SecureStream);
        SecureStream = null;
        return lifetime.DisposeResource();
    }

    internal FtpFaultIsolationResult ShutdownSecureStream()
    {
        return FtpTlsClearControlShutdown.Shutdown(
            SecureStream,
            Request.TransportStream,
            Request.HandshakeTimeoutMilliseconds,
            Request.CancellationToken);
    }

    internal void RequireFreshStream()
    {
        if (Started || Failed || Closed)
        {
            throw new FtpProtocolException(
                "An FTP TLS server stream can authenticate only once.", null);
        }
    }

    internal static void ValidateRequest(FtpTlsServerStreamRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A TLS server stream request is required.", null);
        }
        FtpTlsHandshake.ValidateTransportRequest(
            request.TransportStream, request.Options, request.HandshakeTimeoutMilliseconds);
        if (request.Options == null)
        {
            throw new FtpConfigurationException("TLS server options are required.", null);
        }
        if (request.Options.ServerCertificate == null &&
            request.CertificateSelection == null && request.CertificateProvider == null)
        {
            throw new FtpConfigurationException(
                "A TLS server certificate or selection callback is required.", null);
        }
        if (request.CertificateProvider != null &&
            FtpConfigurationValidator.HasInvalidText(request.HostName))
        {
            throw new FtpConfigurationException(
                "A server certificate HOST context cannot contain control characters.", null);
        }
        if (request.CertificateProvider != null)
        {
            FtpServerCertificatePolicy.ValidateOptionalEndpoints(
                request.LocalEndpoint, request.RemoteEndpoint);
        }
    }
}
