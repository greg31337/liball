using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace tftp;

internal static class TftpStorageCancellation
{
    internal static void ThrowIfReadCancellationRequested(
        CancellationToken cancellationToken,
        FileStream content)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            return;
        }

        content.Dispose();
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal static void ThrowIfPendingWriteCancellationRequested(
        CancellationToken cancellationToken,
        TftpPendingWrite pendingWrite,
        Dictionary<object, TftpPendingWrite> pendingWrites)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            return;
        }

        pendingWrite.AbandonWrite();
        lock (pendingWrites)
        {
            pendingWrites.Remove(pendingWrite.Token);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
