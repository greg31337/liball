using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ftp;

internal class FtpTlsClientStream
{
    internal FtpTlsClientStreamRequest Request;
    internal SslStream? SecureStream;
    internal bool Started;
    internal bool Failed;
    internal bool Closed;
    internal FtpClientCertificatePolicy? CertificatePolicy;

    internal FtpTlsClientStream(FtpTlsClientStreamRequest? request)
    {
        ValidateRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A TLS client stream request is required.", null);
        }

        Request = request;
        SecureStream = null;
        Started = false;
        Failed = false;
        Closed = false;
        CertificatePolicy = null;
        if (request.RemoteEndpoint != null)
        {
            CertificatePolicy = new FtpClientCertificatePolicy(
                request.RemoteEndpoint,
                request.Options,
                request.CertificateValidator);
        }
    }

    internal FtpTlsStreamResult AuthenticateImplicitClient()
    {
        return AuthenticateClient(true);
    }

    internal FtpTlsStreamResult UpgradeExplicitClient(FtpReply? authorizationReply)
    {
        FtpTlsHandshake.RequireExplicitAuthorizationReply(authorizationReply);
        return AuthenticateClient(false);
    }

    internal FtpTlsStreamResult AuthenticateDataClient()
    {
        FtpTlsStreamResult result = AuthenticateClient(false);
        result.ExplicitUpgrade = false;
        result.DataChannel = true;
        result.Diagnostics.ExplicitUpgrade = false;
        result.Diagnostics.DataChannel = true;
        return result;
    }

    internal FtpTlsStreamResult AuthenticateClient(bool implicitTls)
    {
        RequireFreshStream();
        if (Request.TransportStream == null || Request.Options == null)
        {
            throw new FtpConfigurationException(
                "A complete TLS client stream request is required.", null);
        }

        Started = true;
        SecureStream = new SslStream(Request.TransportStream, Request.LeaveTransportOpen);
        SslClientAuthenticationOptions authentication = CreateAuthenticationOptions();
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            FtpTlsHandshake.AuthenticateClient(
                SecureStream,
                authentication,
                Request.HandshakeTimeoutMilliseconds,
                Request.CancellationToken);
            return null;
        });
        if (!operation.Succeeded)
        {
            if (FtpCancellation.IsCancellationRequested(Request.CancellationToken))
            {
                FailAndThrow(new OperationCanceledException(
                    "The FTP TLS client handshake was canceled.",
                    operation.Failure,
                    Request.CancellationToken));
            }
            if (operation.Canceled)
            {
                FailAndThrow(new TimeoutException(
                    "The FTP TLS client handshake timed out.", operation.Failure));
            }
            if (CertificatePolicy != null &&
                CertificatePolicy.ValidationAttempted && !CertificatePolicy.Accepted)
            {
                string message = CertificatePolicy.FailureMessage == null ?
                    "The FTP TLS server certificate was rejected." :
                    CertificatePolicy.FailureMessage;
                FailAndThrow(new FtpTlsException(message, operation.Failure));
            }
            FailAndThrow(operation.Failure);
        }
        SslStream? completedStream = SecureStream;
        if (completedStream == null)
        {
            FailAndThrow(new FtpTlsException(
                "The FTP TLS client handshake completed without a secure stream.", null));
            throw new FtpTlsException(
                "The FTP TLS client handshake completed without a secure stream.", null);
        }

        return FtpTlsHandshake.CreateResult(
            completedStream, false, implicitTls, Request.AllowTlsResume);
    }

    internal SslClientAuthenticationOptions CreateAuthenticationOptions()
    {
        if (Request.Options == null)
        {
            throw new FtpConfigurationException("TLS client options are required.", null);
        }

        SslClientAuthenticationOptions authentication = new SslClientAuthenticationOptions();
        authentication.TargetHost = CertificatePolicy == null ?
            Request.TargetHost : CertificatePolicy.TargetHost;
        authentication.EnabledSslProtocols = Request.Options.Protocols;
        authentication.AllowTlsResume = Request.AllowTlsResume;
        authentication.CertificateRevocationCheckMode =
            CertificatePolicy == null ?
                (Request.Options.CheckCertificateRevocation ?
                    X509RevocationMode.Online : X509RevocationMode.NoCheck) :
                CertificatePolicy.RevocationMode;
        authentication.RemoteCertificateValidationCallback = CertificatePolicy == null ?
            Request.CertificateValidation : CertificatePolicy.CreateValidationCallback();
        X509Certificate2? clientCertificate = CertificatePolicy == null ?
            Request.Options.ClientCertificate : CertificatePolicy.ClientCertificate;
        if (clientCertificate != null)
        {
            X509CertificateCollection certificates = new X509CertificateCollection();
            certificates.Add(clientCertificate);
            authentication.ClientCertificates = certificates;
        }
        return authentication;
    }

    internal void CloseClientTls()
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
                "The FTP TLS client stream could not be closed.", cleanup.Failure);
        }
    }

    internal void ShutdownClientTlsForClearControl()
    {
        if (Closed)
        {
            throw new FtpProtocolException(
                "The FTP TLS client stream is already closed.", null);
        }
        if (!Request.LeaveTransportOpen)
        {
            throw new FtpConfigurationException(
                "CCC requires the client TLS stream to leave the control transport open.", null);
        }

        Closed = true;
        FtpFaultIsolationResult shutdown = ShutdownSecureStream();
        FtpFaultIsolationResult cleanup = DisposeSecureStream();
        if (!shutdown.Succeeded)
        {
            Failed = true;
            throw new FtpTlsException(
                "The FTP TLS client could not send an orderly CCC shutdown.",
                shutdown.Failure);
        }
        if (!cleanup.Succeeded)
        {
            Failed = true;
            throw new FtpTlsException(
                "The FTP TLS client stream could not be released after CCC.",
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
        throw new FtpTlsException("The FTP TLS client handshake failed.", failure);
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
                "An FTP TLS client stream can authenticate only once.", null);
        }
    }

    internal static void ValidateRequest(FtpTlsClientStreamRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A TLS client stream request is required.", null);
        }
        FtpTlsHandshake.ValidateTransportRequest(
            request.TransportStream, request.Options, request.HandshakeTimeoutMilliseconds);
        if (request.RemoteEndpoint == null &&
            (string.IsNullOrWhiteSpace(request.TargetHost) ||
                FtpConfigurationValidator.HasInvalidText(request.TargetHost)))
        {
            throw new FtpConfigurationException(
                "A non-empty TLS client target host without control characters is required.", null);
        }
        if (request.RemoteEndpoint != null)
        {
            FtpConfigurationValidator.ValidateRemoteEndpoint(request.RemoteEndpoint);
        }
    }
}
