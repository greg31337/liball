using System.Net.Sockets;

namespace ftp;

internal static class FtpSocketSettings
{
    internal const int MaximumBufferBytes = 67108864;
    internal const int MaximumTimeoutMilliseconds = 86400000;

    internal static FtpSocketOptions CreateSnapshot(FtpSocketOptions? options)
    {
        Validate(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Socket options are required.", null);
        }

        FtpSocketOptions snapshot = new FtpSocketOptions();
        snapshot.NoDelay = options.NoDelay;
        snapshot.KeepAlive = options.KeepAlive;
        snapshot.ReceiveBufferBytes = options.ReceiveBufferBytes;
        snapshot.SendBufferBytes = options.SendBufferBytes;
        snapshot.ReceiveTimeoutMilliseconds = options.ReceiveTimeoutMilliseconds;
        snapshot.SendTimeoutMilliseconds = options.SendTimeoutMilliseconds;
        snapshot.LingerEnabled = options.LingerEnabled;
        snapshot.LingerSeconds = options.LingerSeconds;
        return snapshot;
    }

    internal static void Validate(FtpSocketOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Socket options are required.", null);
        }

        if (options.ReceiveBufferBytes < 0 ||
            options.ReceiveBufferBytes > MaximumBufferBytes ||
            options.SendBufferBytes < 0 ||
            options.SendBufferBytes > MaximumBufferBytes)
        {
            throw new FtpConfigurationException(
                "Socket buffer sizes must be zero or within the supported bound.", null);
        }

        if (options.ReceiveTimeoutMilliseconds < 0 ||
            options.ReceiveTimeoutMilliseconds > MaximumTimeoutMilliseconds ||
            options.SendTimeoutMilliseconds < 0 ||
            options.SendTimeoutMilliseconds > MaximumTimeoutMilliseconds)
        {
            throw new FtpConfigurationException(
                "Socket timeouts must be zero or within the supported bound.", null);
        }

        if (options.LingerSeconds < 0 || options.LingerSeconds > 65535)
        {
            throw new FtpConfigurationException(
                "Socket linger duration must be between zero and 65535 seconds.", null);
        }

        if (!options.LingerEnabled && options.LingerSeconds != 0)
        {
            throw new FtpConfigurationException(
                "A socket linger duration requires linger to be enabled.", null);
        }
    }

    internal static void ApplyConnectedSocket(
        Socket? socket,
        FtpSocketOptions? options)
    {
        if (socket == null)
        {
            throw new FtpConfigurationException("A TCP socket is required.", null);
        }

        Validate(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Socket options are required.", null);
        }

        if (socket.SocketType != SocketType.Stream || socket.ProtocolType != ProtocolType.Tcp)
        {
            throw new FtpConfigurationException(
                "FTP socket settings require a TCP stream socket.", null);
        }

        FtpFaultIsolationResult result = FtpFaultIsolationRunner.RunOperation(() =>
        {
            socket.NoDelay = options.NoDelay;
            socket.SetSocketOption(
                SocketOptionLevel.Socket, SocketOptionName.KeepAlive, options.KeepAlive);
            if (options.ReceiveBufferBytes != 0)
            {
                socket.ReceiveBufferSize = options.ReceiveBufferBytes;
            }

            if (options.SendBufferBytes != 0)
            {
                socket.SendBufferSize = options.SendBufferBytes;
            }

            socket.ReceiveTimeout = options.ReceiveTimeoutMilliseconds;
            socket.SendTimeout = options.SendTimeoutMilliseconds;
            socket.LingerState = new LingerOption(
                options.LingerEnabled, options.LingerSeconds);
            return null;
        });
        if (!result.Succeeded)
        {
            throw new FtpConfigurationException(
                "The requested TCP socket settings are not supported.", result.Failure);
        }
    }
}
