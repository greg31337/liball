using System;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal static class FtpTlsClearControlShutdown
{
    internal static FtpFaultIsolationResult Shutdown(
        SslStream? secureStream,
        Stream? transportStream,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        if (secureStream == null)
        {
            FtpFaultIsolationResult emptyResult = new FtpFaultIsolationResult();
            emptyResult.Succeeded = true;
            return emptyResult;
        }
        if (transportStream == null)
        {
            FtpFaultIsolationResult missing = new FtpFaultIsolationResult();
            missing.Failure = new FtpConfigurationException(
                "The FTP TLS transport stream is required for CCC.", null);
            return missing;
        }

        FtpFaultIsolationResult shutdown = FtpFaultIsolationRunner.RunOperation(() =>
        {
            Task shutdownTask = secureStream.ShutdownAsync();
            bool completed = shutdownTask.Wait(
                timeoutMilliseconds, cancellationToken);
            if (!completed)
            {
                throw new TimeoutException(
                    "The FTP TLS close notification timed out.");
            }
            shutdownTask.GetAwaiter().GetResult();
            ConsumePeerCloseNotification(
                secureStream,
                timeoutMilliseconds,
                cancellationToken);
            return null;
        });
        return shutdown;
    }

    internal static void ConsumePeerCloseNotification(
        SslStream secureStream,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource readCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
        if (timeoutMilliseconds != Timeout.Infinite)
        {
            readCancellation.CancelAfter(timeoutMilliseconds);
        }
        byte[] buffer = new byte[1];
        int read = secureStream.ReadAsync(
            buffer, 0, buffer.Length, readCancellation.Token)
            .GetAwaiter().GetResult();
        readCancellation.Dispose();
        if (read != 0)
        {
            throw new FtpProtocolException(
                "The FTP peer sent protected data after the TLS close request.",
                null);
        }
    }
}
