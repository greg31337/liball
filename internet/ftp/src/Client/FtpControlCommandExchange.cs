using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ftp;

internal class FtpControlCommandExchange
{
    internal FtpClientOptions Options;

    internal FtpControlCommandExchange(FtpClientOptions options)
    {
        Options = options;
    }

    internal FtpReply ExecuteCommand(
        FtpControlConnection connection,
        FtpCommand command)
    {
        WriteCommand(connection, command);
        FtpDeadline readDeadline = FtpTimeout.StartDeadline(
            Options.Timeouts.ControlReadTimeoutMilliseconds);
        return FtpControlReplyReader.ReadReply(
            connection.Stream,
            connection.Socket,
            readDeadline,
            Options.Timeouts.CancellationToken,
            Options.StrictReplyParsing);
    }

    internal void WriteCommand(
        FtpControlConnection connection,
        FtpCommand command)
    {
        Stream? stream = connection.Stream;
        if (stream == null || connection.Socket == null)
        {
            throw new FtpConnectionException(
                "An open FTP control stream is required to write a command.", null);
        }

        byte[] commandBytes = FtpCommandWriter.FormatEncodedCommand(
            command, connection.Utf8Enabled ? Encoding.UTF8 : Encoding.ASCII);
        CancellationToken cancellationToken = Options.Timeouts.CancellationToken;
        FtpDeadline deadline = FtpTimeout.StartDeadline(
            Options.Timeouts.ControlWriteTimeoutMilliseconds);
        TransferPump.CheckOperationState(cancellationToken, deadline);
        FtpFaultIsolationResult writeResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            stream.WriteTimeout = FtpTimeout.GetRemainingWaitMilliseconds(deadline);
            using (CancellationTokenRegistration cancellationRegistration =
                cancellationToken.Register(() =>
                {
                    connection.Socket.Dispose();
                }))
            {
                stream.Write(commandBytes, 0, commandBytes.Length);
                stream.Flush();
                return null;
            }
        });
        if (!writeResult.Succeeded)
        {
            ThrowWriteFailure(writeResult, cancellationToken, deadline);
        }
    }

    internal void ThrowWriteFailure(
        FtpFaultIsolationResult writeResult,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The FTP control command write was canceled.",
                writeResult.Failure,
                cancellationToken);
        }
        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException(
                "The FTP control command write timed out.", writeResult.Failure);
        }

        throw new FtpConnectionException(
            "The FTP control command could not be written.", writeResult.Failure);
    }
}
