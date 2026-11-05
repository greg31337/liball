using System.IO;
using System.Threading;

namespace ftp;

internal class FtpTransferHashRequest
{
    internal Stream? Source;
    internal string? Algorithm;
    internal int BufferBytes;
    internal CancellationToken CancellationToken;
    internal int DeadlineMilliseconds;
    internal bool LeaveOpen;

    internal FtpTransferHashRequest()
    {
        Source = null;
        Algorithm = null;
        BufferBytes = FtpDefaults.TransferBufferBytes;
        CancellationToken = default(CancellationToken);
        DeadlineMilliseconds = Timeout.Infinite;
        LeaveOpen = true;
    }
}
