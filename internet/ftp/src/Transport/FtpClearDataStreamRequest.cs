using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ftp;

internal class FtpClearDataStreamRequest
{
    internal FtpCommandKind CommandKind;
    internal Stream? LocalStream;
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
    internal int ConnectionTimeoutMilliseconds;
    internal int TransferTimeoutMilliseconds;
    internal bool LeaveLocalStreamOpen;
    internal Func<Socket, Stream>? DataStreamFactory;
    internal Action? DataStreamShutdown;

    internal FtpClearDataStreamRequest()
    {
        CommandKind = FtpCommandKind.Unknown;
        LocalStream = null;
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
        ConnectionTimeoutMilliseconds = FtpDefaults.DataConnectionTimeoutMilliseconds;
        TransferTimeoutMilliseconds = Timeout.Infinite;
        LeaveLocalStreamOpen = true;
        DataStreamFactory = null;
        DataStreamShutdown = null;
    }
}
