using System;
using System.Net.Security;
using System.Threading;

namespace ftp;

internal static class FtpTlsHandshake
{
    internal static void AuthenticateClient(
        SslStream stream,
        SslClientAuthenticationOptions options,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        FtpDeadline deadline = FtpTimeout.StartDeadline(timeoutMilliseconds);
        CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutMilliseconds != Timeout.Infinite)
        {
            linkedCancellation.CancelAfter(timeoutMilliseconds);
        }

        FtpFaultIsolationResult handshake = FtpFaultIsolationRunner.RunOperation(() =>
        {
            stream.AuthenticateAsClientAsync(
                options, linkedCancellation.Token).GetAwaiter().GetResult();
            return null;
        });
        bool callerCanceled = FtpCancellation.IsCancellationRequested(cancellationToken);
        bool timedOut = !callerCanceled &&
            (FtpTimeout.HasDeadlineExpired(deadline) || linkedCancellation.IsCancellationRequested);
        FtpResourceLifetime cancellationLifetime = new FtpResourceLifetime(linkedCancellation);
        FtpFaultIsolationResult cancellationCleanup = cancellationLifetime.DisposeResource();
        ThrowHandshakeFailure(
            handshake,
            cancellationCleanup,
            callerCanceled,
            timedOut,
            cancellationToken,
            "client");
    }

    internal static void AuthenticateServer(
        SslStream stream,
        SslServerAuthenticationOptions options,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        FtpDeadline deadline = FtpTimeout.StartDeadline(timeoutMilliseconds);
        CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutMilliseconds != Timeout.Infinite)
        {
            linkedCancellation.CancelAfter(timeoutMilliseconds);
        }

        FtpFaultIsolationResult handshake = FtpFaultIsolationRunner.RunOperation(() =>
        {
            stream.AuthenticateAsServerAsync(
                options, linkedCancellation.Token).GetAwaiter().GetResult();
            return null;
        });
        bool callerCanceled = FtpCancellation.IsCancellationRequested(cancellationToken);
        bool timedOut = !callerCanceled &&
            (FtpTimeout.HasDeadlineExpired(deadline) || linkedCancellation.IsCancellationRequested);
        FtpResourceLifetime cancellationLifetime = new FtpResourceLifetime(linkedCancellation);
        FtpFaultIsolationResult cancellationCleanup = cancellationLifetime.DisposeResource();
        ThrowHandshakeFailure(
            handshake,
            cancellationCleanup,
            callerCanceled,
            timedOut,
            cancellationToken,
            "server");
    }

    internal static void AuthenticateSelectedServer(
        SslStream stream,
        ServerOptionsSelectionCallback optionsSelection,
        object state,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        FtpDeadline deadline = FtpTimeout.StartDeadline(timeoutMilliseconds);
        CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutMilliseconds != Timeout.Infinite)
        {
            linkedCancellation.CancelAfter(timeoutMilliseconds);
        }

        FtpFaultIsolationResult handshake = FtpFaultIsolationRunner.RunOperation(() =>
        {
            stream.AuthenticateAsServerAsync(
                optionsSelection,
                state,
                linkedCancellation.Token).GetAwaiter().GetResult();
            return null;
        });
        bool callerCanceled = FtpCancellation.IsCancellationRequested(cancellationToken);
        bool timedOut = !callerCanceled &&
            (FtpTimeout.HasDeadlineExpired(deadline) || linkedCancellation.IsCancellationRequested);
        FtpResourceLifetime cancellationLifetime = new FtpResourceLifetime(linkedCancellation);
        FtpFaultIsolationResult cancellationCleanup = cancellationLifetime.DisposeResource();
        ThrowHandshakeFailure(
            handshake,
            cancellationCleanup,
            callerCanceled,
            timedOut,
            cancellationToken,
            "server");
    }

    internal static void ThrowHandshakeFailure(
        FtpFaultIsolationResult handshake,
        FtpFaultIsolationResult cancellationCleanup,
        bool callerCanceled,
        bool timedOut,
        CancellationToken cancellationToken,
        string role)
    {
        if (callerCanceled)
        {
            throw new OperationCanceledException(
                $"The FTP TLS {role} handshake was canceled.",
                handshake.Failure,
                cancellationToken);
        }
        if (timedOut)
        {
            throw new TimeoutException(
                $"The FTP TLS {role} handshake timed out.", handshake.Failure);
        }
        if (!handshake.Succeeded)
        {
            throw new FtpTlsException(
                $"The FTP TLS {role} handshake failed.", handshake.Failure);
        }
        if (!cancellationCleanup.Succeeded)
        {
            throw new FtpTlsException(
                $"The FTP TLS {role} handshake cancellation state could not be released.",
                cancellationCleanup.Failure);
        }
    }

    internal static FtpTlsStreamResult CreateResult(
        SslStream stream,
        bool server,
        bool implicitTls,
        bool tlsResumeAllowed)
    {
        FtpTlsStreamResult result = new FtpTlsStreamResult();
        result.Stream = stream;
        result.Authenticated = stream.IsAuthenticated;
        result.Server = server;
        result.ImplicitTls = implicitTls;
        result.ExplicitUpgrade = !implicitTls;
        result.TlsResumeAllowed = tlsResumeAllowed;
        result.Encrypted = stream.IsEncrypted;
        result.Signed = stream.IsSigned;
        result.MutuallyAuthenticated = stream.IsMutuallyAuthenticated;
        result.Protocol = stream.SslProtocol;
        result.Diagnostics = FtpTlsDiagnosticsFactory.CreateDiagnostics(
            stream, server, implicitTls, !implicitTls, false, tlsResumeAllowed);
        return result;
    }

    internal static void ValidateTransportRequest(
        System.IO.Stream? transportStream,
        FtpTlsOptions? options,
        int timeoutMilliseconds)
    {
        if (transportStream == null || options == null)
        {
            throw new FtpConfigurationException(
                "A transport stream and TLS options are required.", null);
        }
        if (!transportStream.CanRead || !transportStream.CanWrite)
        {
            throw new FtpConfigurationException(
                "An FTP TLS transport stream must be readable and writable.", null);
        }
        if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
        {
            throw new FtpConfigurationException(
                "A TLS handshake timeout must be positive or infinite.", null);
        }

        FtpConfigurationValidator.ValidateTlsOptions(options);
    }

    internal static void RequireExplicitAuthorizationReply(FtpReply? reply)
    {
        FtpReplyExtractor.RequireExpectedCode(
            reply, FtpReplyCodes.SecurityExchangeComplete, "AUTH TLS");
    }
}
