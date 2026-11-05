using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ftp;

internal static class FtpControlReplyReader
{
    internal static FtpReply ReadReply(
        Stream? stream,
        Socket? socket,
        FtpDeadline deadline,
        CancellationToken cancellationToken,
        bool strict)
    {
        if (stream == null || socket == null)
        {
            throw new FtpConnectionException(
                "An open FTP control stream is required to read a reply.", null);
        }

        TransferPump.CheckOperationState(cancellationToken, deadline);
        int remainingMilliseconds = FtpTimeout.GetRemainingWaitMilliseconds(deadline);
        FtpFaultIsolationResult readResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            stream.ReadTimeout = remainingMilliseconds;
            using (CancellationTokenRegistration cancellationRegistration =
                cancellationToken.Register(() =>
                {
                    socket.Dispose();
                }))
            {
                FtpReplyStreamReader streamReader = new FtpReplyStreamReader(
                    FtpDefaults.MaximumReplyBytes,
                    FtpDefaults.MaximumReplyLines,
                    FtpDefaults.MaximumControlLineBytes,
                    strict);
                byte[] replyBytes = streamReader.ReadReplyBytes(stream);
                FtpReplyParser parser = new FtpReplyParser(
                    FtpDefaults.MaximumReplyBytes,
                    FtpDefaults.MaximumReplyLines,
                    strict);
                return parser.ParseEncodedReply(replyBytes, Encoding.UTF8);
            }
        });

        if (readResult.Succeeded)
        {
            FtpReply? reply = readResult.Value as FtpReply;
            if (reply != null)
            {
                return reply;
            }

            throw new FtpProtocolException(
                "The FTP greeting read completed without a reply.", null);
        }

        ThrowReadFailure(readResult, cancellationToken, deadline);
        throw new FtpConnectionException(
            "The FTP greeting read did not reach a terminal result.", null);
    }

    internal static void ThrowReadFailure(
        FtpFaultIsolationResult readResult,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The FTP greeting read was canceled.",
                readResult.Failure,
                cancellationToken);
        }

        OperationCanceledException? canceledFailure =
            readResult.Failure as OperationCanceledException;
        if (FtpTimeout.HasDeadlineExpired(deadline) || canceledFailure != null)
        {
            throw new TimeoutException(
                "The FTP greeting read timed out.", readResult.Failure);
        }

        FtpProtocolException? protocolFailure =
            readResult.Failure as FtpProtocolException;
        if (protocolFailure != null)
        {
            throw protocolFailure;
        }

        throw new FtpConnectionException(
            "The FTP greeting could not be read from the control connection.",
            readResult.Failure);
    }
}
