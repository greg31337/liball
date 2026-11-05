using System;
using System.IO;
using System.Threading;

namespace ftp;

internal class FtpTransferPumpRequest
{
    internal Stream? Source;
    internal Stream? Destination;
    internal FtpTransferOptions? Options;
    internal IFtpTransferPumpTransform? Transform;
    internal Action<FtpTransferProgress>? ProgressCallback;
    internal FtpTransferDirection Direction;
    internal string? Path;
    internal bool HasExpectedBytes;
    internal long ExpectedBytes;
    internal long InitialLogicalBytes;
    internal long InitialWireBytes;
    internal bool HasMaximumBytes;
    internal long MaximumBytes;
    internal bool HasDestinationMaximumBytes;
    internal long DestinationMaximumBytes;
    internal bool HasBytesPerSecond;
    internal long BytesPerSecond;
    internal int DeadlineMilliseconds;
    internal bool LeaveSourceOpen;
    internal bool LeaveDestinationOpen;

    internal FtpTransferPumpRequest()
    {
        Source = null;
        Destination = null;
        Options = new FtpTransferOptions();
        Transform = null;
        ProgressCallback = null;
        Direction = FtpTransferDirection.Download;
        Path = null;
        HasExpectedBytes = false;
        ExpectedBytes = 0;
        InitialLogicalBytes = 0;
        InitialWireBytes = 0;
        HasMaximumBytes = false;
        MaximumBytes = 0;
        HasDestinationMaximumBytes = false;
        DestinationMaximumBytes = 0;
        HasBytesPerSecond = false;
        BytesPerSecond = 0;
        DeadlineMilliseconds = Timeout.Infinite;
        LeaveSourceOpen = true;
        LeaveDestinationOpen = true;
    }
}
