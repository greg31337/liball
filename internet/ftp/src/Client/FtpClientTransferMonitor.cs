using System;
using System.Collections.Generic;

namespace ftp;

internal class FtpClientTransferMonitor
{
    internal FtpClientState ClientState;
    internal IFtpTransferObserver? Observer;
    internal FtpTransferOptions Options;
    internal List<FtpCommandResult> KeepAliveCommands;
    internal bool Started;
    internal bool KeepAliveEnabled;
    internal long LastKeepAliveMilliseconds;

    internal FtpClientTransferMonitor(
        FtpClientState clientState,
        IFtpTransferObserver? observer,
        FtpTransferOptions options)
    {
        ClientState = clientState;
        Observer = observer;
        Options = options;
        KeepAliveCommands = new List<FtpCommandResult>();
        Started = false;
        KeepAliveEnabled = options.EnableKeepAlive;
        LastKeepAliveMilliseconds = 0;
    }

    internal void ReportProgress(FtpTransferProgress progress)
    {
        long currentMilliseconds = Environment.TickCount64;
        if (!Started)
        {
            Started = true;
            LastKeepAliveMilliseconds = currentMilliseconds;
            NotifyStarted(progress);
            return;
        }

        NotifyProgress(progress);
        if (progress.Completed ||
            FtpCancellation.IsCancellationRequested(
                Options.Timeouts.CancellationToken))
        {
            return;
        }
        if (KeepAliveEnabled &&
            currentMilliseconds - LastKeepAliveMilliseconds >=
                Options.KeepAliveIntervalMilliseconds)
        {
            SendKeepAlive(currentMilliseconds);
        }
    }

    internal void NotifyStarted(FtpTransferProgress progress)
    {
        if (Observer == null)
        {
            return;
        }
        IFtpTransferObserver observer = Observer;
        FtpFaultIsolationRunner.RunOperation(() =>
        {
            observer.ObserveTransferStarted(progress);
            return null;
        });
    }

    internal void NotifyProgress(FtpTransferProgress progress)
    {
        if (Observer == null)
        {
            return;
        }
        IFtpTransferObserver observer = Observer;
        FtpFaultIsolationRunner.RunOperation(() =>
        {
            observer.ObserveTransferProgress(progress);
            return null;
        });
    }

    internal void SendKeepAlive(long currentMilliseconds)
    {
        LastKeepAliveMilliseconds = currentMilliseconds;
        FtpClientCommandExecutor executor =
            new FtpClientCommandExecutor(ClientState);
        FtpCommandResult result = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(FtpCommandKind.Noop, null));
        KeepAliveCommands.Add(result);
        if (!result.ConnectionClosed)
        {
            ClientState.UpdateReconnectState();
        }
        if (!result.Succeeded || result.ConnectionClosed)
        {
            KeepAliveEnabled = false;
        }
    }

    internal void AppendKeepAliveCommands(List<FtpCommandResult> commands)
    {
        int index = 0;
        while (index < KeepAliveCommands.Count)
        {
            commands.Add(KeepAliveCommands[index]);
            index++;
        }
    }

    internal static void NotifyCompleted(
        IFtpTransferObserver? observer,
        FtpTransferResult result)
    {
        if (observer == null)
        {
            return;
        }
        FtpFaultIsolationRunner.RunOperation(() =>
        {
            observer.ObserveTransferCompleted(result);
            return null;
        });
    }
}
