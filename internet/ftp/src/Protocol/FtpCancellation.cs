using System.Threading;

namespace ftp;

internal static class FtpCancellation
{
    internal static bool IsCancellationRequested(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested;
    }

    internal static void ThrowIfCancellationRequested(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
    }
}
