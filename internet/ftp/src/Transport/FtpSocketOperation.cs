using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal static class FtpSocketOperation
{
    internal static Socket Connect(
        IPAddress? address,
        int port,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        return ConnectConfigured(
            address,
            port,
            timeoutMilliseconds,
            cancellationToken,
            new FtpSocketOptions());
    }

    internal static Socket ConnectConfigured(
        IPAddress? address,
        int port,
        int timeoutMilliseconds,
        CancellationToken cancellationToken,
        FtpSocketOptions? options)
    {
        return ConnectBoundConfigured(
            address,
            port,
            null,
            0,
            timeoutMilliseconds,
            cancellationToken,
            options);
    }

    internal static Socket ConnectBoundConfigured(
        IPAddress? address,
        int port,
        IPAddress? bindAddress,
        int bindPort,
        int timeoutMilliseconds,
        CancellationToken cancellationToken,
        FtpSocketOptions? options)
    {
        ValidateConnectRequest(address, port, timeoutMilliseconds);
        ValidateBindRequest(address, bindAddress, bindPort);
        FtpSocketOptions socketOptions = FtpSocketSettings.CreateSnapshot(options);
        if (address == null)
        {
            throw new FtpConfigurationException("A numeric socket address is required.", null);
        }

        FtpDeadline deadline = FtpTimeout.StartDeadline(timeoutMilliseconds);
        TransferPump.CheckOperationState(cancellationToken, deadline);
        Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        FtpResourceLifetime lifetime = new FtpResourceLifetime(socket);
        FtpFaultIsolationResult configuration = FtpFaultIsolationRunner.RunOperation(() =>
        {
            FtpSocketSettings.ApplyConnectedSocket(socket, socketOptions);
            if (bindAddress != null)
            {
                socket.Bind(new IPEndPoint(bindAddress, bindPort));
            }
            return null;
        });
        if (!configuration.Succeeded)
        {
            FtpFaultIsolationResult configurationCleanup = lifetime.DisposeResource();
            Exception? failure = configuration.Failure == null ?
                configurationCleanup.Failure : configuration.Failure;
            throw new FtpConfigurationException(
                "The TCP socket could not be configured before connection.", failure);
        }

        FtpFaultIsolationResult connection = FtpFaultIsolationRunner.RunOperation(() =>
        {
            TransferPump.CheckOperationState(cancellationToken, deadline);
            using (CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                int remainingMilliseconds = FtpTimeout.GetRemainingWaitMilliseconds(deadline);
                if (remainingMilliseconds != Timeout.Infinite)
                {
                    linkedCancellation.CancelAfter(remainingMilliseconds);
                }

                IPEndPoint endpoint = new IPEndPoint(address, port);
                ValueTask connectOperation = socket.ConnectAsync(endpoint, linkedCancellation.Token);
                Task connectTask = connectOperation.AsTask();
                connectTask.GetAwaiter().GetResult();
            }

            TransferPump.CheckOperationState(cancellationToken, deadline);
            return null;
        });

        bool canceled = FtpCancellation.IsCancellationRequested(cancellationToken);
        bool timedOut = FtpTimeout.HasDeadlineExpired(deadline);
        if (connection.Succeeded && !canceled && !timedOut)
        {
            lifetime.ReleaseResource();
            return socket;
        }

        FtpFaultIsolationResult cleanup = lifetime.DisposeResource();
        ThrowConnectFailure(connection, cleanup, cancellationToken, deadline);
        throw new FtpConnectionException("The FTP socket connection did not reach a terminal result.", null);
    }

    internal static void ValidateBindRequest(
        IPAddress? address,
        IPAddress? bindAddress,
        int bindPort)
    {
        if (bindPort < 0 || bindPort > 65535)
        {
            throw new FtpConfigurationException(
                "A local socket bind port must be between 0 and 65535.", null);
        }

        if (bindAddress == null)
        {
            if (bindPort != 0)
            {
                throw new FtpConfigurationException(
                    "A local socket bind port requires a numeric bind address.", null);
            }

            return;
        }

        if (address == null || bindAddress.AddressFamily != address.AddressFamily)
        {
            throw new FtpConfigurationException(
                "The local and remote socket address families must match.", null);
        }
    }

    internal static void ValidateConnectRequest(
        IPAddress? address,
        int port,
        int timeoutMilliseconds)
    {
        if (address == null ||
            (address.AddressFamily != AddressFamily.InterNetwork &&
                address.AddressFamily != AddressFamily.InterNetworkV6))
        {
            throw new FtpConfigurationException("A numeric IPv4 or IPv6 socket address is required.", null);
        }

        if (port < 1 || port > 65535)
        {
            throw new FtpConfigurationException("A socket port must be between 1 and 65535.", null);
        }

        if (timeoutMilliseconds < Timeout.Infinite)
        {
            throw new FtpConfigurationException(
                "A socket timeout must be non-negative or Timeout.Infinite.", null);
        }
    }

    internal static void ThrowConnectFailure(
        FtpFaultIsolationResult connection,
        FtpFaultIsolationResult cleanup,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The FTP socket connection was canceled.", connection.Failure, cancellationToken);
        }

        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException("The FTP socket connection timed out.", connection.Failure);
        }

        if (!connection.Succeeded)
        {
            throw new FtpConnectionException("The FTP socket connection failed.", connection.Failure);
        }

        if (!cleanup.Succeeded)
        {
            throw new FtpConnectionException("The failed FTP socket could not be closed.", cleanup.Failure);
        }
    }
}
