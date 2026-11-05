using System;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpServerPendingTransfer
{
    internal FtpCommand Command;
    internal Func<FtpReply> Operation;
    internal CancellationTokenSource Cancellation;
    internal Task<object?>? Worker;
    internal bool Started;
    internal bool Aborted;

    internal FtpServerPendingTransfer(
        FtpCommand command,
        Func<FtpReply> operation,
        CancellationTokenSource cancellation)
    {
        Command = command;
        Operation = operation;
        Cancellation = cancellation;
        Worker = null;
        Started = false;
        Aborted = false;
    }
}
