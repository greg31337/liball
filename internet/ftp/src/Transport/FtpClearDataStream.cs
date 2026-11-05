using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ftp;

internal class FtpClearDataStream
{
    internal FtpDataConnectionCoordinator Coordinator;
    internal FtpClearDataStreamRequest? Request;
    internal FtpClearDataStreamResult Result;
    internal Stream? DataStream;
    internal bool Started;
    internal bool Sending;
    internal bool DataComplete;
    internal bool FinalReplyApplied;

    internal FtpClearDataStream(FtpDataConnectionCoordinator? coordinator)
    {
        if (coordinator == null)
        {
            throw new FtpConfigurationException(
                "A data-connection coordinator is required.", null);
        }

        Coordinator = coordinator;
        Request = null;
        Result = new FtpClearDataStreamResult();
        DataStream = null;
        Started = false;
        Sending = false;
        DataComplete = false;
        FinalReplyApplied = false;
    }

    internal FtpReply BeginSend(FtpClearDataStreamRequest? request)
    {
        BeginTransfer(request, true);
        return Result.PreliminaryReply;
    }

    internal FtpReply BeginReceive(FtpClearDataStreamRequest? request)
    {
        BeginTransfer(request, false);
        return Result.PreliminaryReply;
    }

    internal FtpTransferPumpResult ExecuteSendData()
    {
        ValidateExecution(true);
        ExecuteDataTransfer(true);
        if (Result.PumpResult == null)
        {
            throw new FtpTransferException(
                "The clear data send completed without transfer accounting.", null);
        }

        return Result.PumpResult;
    }

    internal FtpTransferPumpResult ExecuteReceiveData()
    {
        ValidateExecution(false);
        ExecuteDataTransfer(false);
        if (Result.PumpResult == null)
        {
            throw new FtpTransferException(
                "The clear data receive completed without transfer accounting.", null);
        }

        return Result.PumpResult;
    }

    internal FtpReply CreateFinalReply()
    {
        ValidateFinalReplyAvailability();
        FtpReply reply = FtpEndpointCodec.CreateEndpointReply(
            FtpReplyCodes.ClosingDataConnection,
            "Closing data connection; transfer complete");
        ApplyTerminalReply(reply);
        return reply;
    }

    internal void ApplyFinalReply(FtpReply? reply)
    {
        ValidateFinalReplyAvailability();
        if (reply == null)
        {
            throw new FtpProtocolException(
                "A terminal FTP data reply is required.", null);
        }

        ApplyTerminalReply(reply);
    }

    internal void BeginTransfer(
        FtpClearDataStreamRequest? request,
        bool sending)
    {
        if (Started)
        {
            throw new FtpProtocolException(
                "A clear FTP data-stream operation already started.", null);
        }

        ValidateRequest(request, sending);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A clear data-stream request is required.", null);
        }

        Request = request;
        Sending = sending;
        Started = true;
        DataComplete = false;
        FinalReplyApplied = false;
        Result = new FtpClearDataStreamResult();
        Result.Sending = sending;
        Result.PreliminaryReply = Coordinator.BeginTransfer();
    }

    internal void ExecuteDataTransfer(bool sending)
    {
        FtpClearDataStreamRequest request = RequireRequest();
        if (request.Options == null || request.LocalStream == null)
        {
            throw new FtpConfigurationException(
                "A complete clear data-stream request is required.", null);
        }

        CancellationToken cancellationToken = request.Options.Timeouts.CancellationToken;
        Socket socket = Coordinator.OpenDataConnection(
            request.ConnectionTimeoutMilliseconds, cancellationToken);
        FtpDeadline deadline = FtpTimeout.StartDeadline(
            request.TransferTimeoutMilliseconds);
        FtpTransferPumpRequest pumpRequest = CreatePumpRequest(request, sending);
        Stream? modeZStream = null;
        FtpFaultIsolationResult transferResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            if (request.DataStreamFactory == null)
            {
                DataStream = new NetworkStream(socket, false);
            }
            else
            {
                DataStream = request.DataStreamFactory.Invoke(socket);
            }
            if (DataStream == null || !DataStream.CanRead || !DataStream.CanWrite)
            {
                throw new FtpConfigurationException(
                    "A readable and writable FTP data stream is required.", null);
            }
            Stream transferStream = DataStream;
            if (request.Options.Mode == FtpTransferMode.Deflate)
            {
                modeZStream = sending ?
                    ModeZCodec.CreateEncodingStream(DataStream) :
                    ModeZCodec.CreateDecodingStream(DataStream);
                transferStream = modeZStream;
            }
            if (sending)
            {
                pumpRequest.Source = request.LocalStream;
                pumpRequest.Destination = transferStream;
            }
            else
            {
                pumpRequest.Source = transferStream;
                pumpRequest.Destination = request.LocalStream;
            }

            Result.PumpResult = TransferPump.Execute(pumpRequest);
            return null;
        });

        FtpResourceLifetime modeZLifetime =
            new FtpResourceLifetime(modeZStream);
        FtpFaultIsolationResult modeZCleanup =
            modeZLifetime.DisposeResource();
        FtpFaultIsolationResult streamShutdown =
            CreateSuccessfulResult();
        if (transferResult.Succeeded && modeZCleanup.Succeeded &&
            request.DataStreamShutdown != null)
        {
            streamShutdown = FtpFaultIsolationRunner.RunOperation(() =>
            {
                request.DataStreamShutdown.Invoke();
                return null;
            });
        }
        FtpResourceLifetime streamLifetime = new FtpResourceLifetime(DataStream);
        DataStream = null;
        FtpFaultIsolationResult streamCleanup = streamLifetime.DisposeResource();
        if (!transferResult.Succeeded || !modeZCleanup.Succeeded ||
            !streamShutdown.Succeeded || !streamCleanup.Succeeded ||
            FtpCancellation.IsCancellationRequested(cancellationToken) ||
            FtpTimeout.HasDeadlineExpired(deadline))
        {
            FtpFaultIsolationResult abortResult = FtpFaultIsolationRunner.RunOperation(() =>
            {
                Coordinator.AbortTransfer();
                return null;
            });
            ThrowTransferFailure(
                transferResult,
                modeZCleanup,
                streamShutdown,
                streamCleanup,
                abortResult,
                cancellationToken,
                deadline);
        }

        if (sending)
        {
            Coordinator.ShutdownWriteSide();
        }

        Coordinator.CompleteTransfer();
        DataComplete = true;
        Result.DataConnectionClosed = true;
    }

    internal FtpTransferPumpRequest CreatePumpRequest(
        FtpClearDataStreamRequest request,
        bool sending)
    {
        FtpTransferPumpRequest pumpRequest = new FtpTransferPumpRequest();
        pumpRequest.Options = request.Options;
        pumpRequest.Transform = request.Transform;
        pumpRequest.ProgressCallback = request.ProgressCallback;
        pumpRequest.Direction = request.Direction;
        pumpRequest.Path = request.Path;
        pumpRequest.HasExpectedBytes = request.HasExpectedBytes;
        pumpRequest.ExpectedBytes = request.ExpectedBytes;
        pumpRequest.InitialLogicalBytes = request.InitialLogicalBytes;
        pumpRequest.InitialWireBytes = request.InitialWireBytes;
        pumpRequest.HasMaximumBytes = request.HasMaximumBytes;
        pumpRequest.MaximumBytes = request.MaximumBytes;
        pumpRequest.HasDestinationMaximumBytes =
            request.HasDestinationMaximumBytes;
        pumpRequest.DestinationMaximumBytes =
            request.DestinationMaximumBytes;
        pumpRequest.HasBytesPerSecond = request.HasBytesPerSecond;
        pumpRequest.BytesPerSecond = request.BytesPerSecond;
        pumpRequest.DeadlineMilliseconds = request.TransferTimeoutMilliseconds;
        pumpRequest.LeaveSourceOpen = sending ? request.LeaveLocalStreamOpen : true;
        pumpRequest.LeaveDestinationOpen = sending ? true : request.LeaveLocalStreamOpen;
        return pumpRequest;
    }

    internal void ApplyTerminalReply(FtpReply reply)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireReply(
            reply, "clear data terminal");
        FtpReplyClassification classification = FtpReplyClassifier.ClassifyCode(
            checkedReply.Code);
        bool succeeded = checkedReply.Code == FtpReplyCodes.ClosingDataConnection ||
            checkedReply.Code == FtpReplyCodes.RequestedFileActionOkay;
        bool failed = classification.TransientFailure || classification.PermanentFailure;
        if (!succeeded && !failed)
        {
            throw new FtpProtocolException(
                "A clear FTP data transfer requires a terminal completion or failure reply.",
                null);
        }

        Result.FinalReply = checkedReply;
        Result.Succeeded = succeeded;
        FinalReplyApplied = true;
    }

    internal void ValidateFinalReplyAvailability()
    {
        if (!DataComplete || !Result.DataConnectionClosed)
        {
            throw new FtpProtocolException(
                "A final FTP reply is unavailable until the data connection closes.", null);
        }

        if (FinalReplyApplied)
        {
            throw new FtpProtocolException(
                "The clear FTP data transfer already has a final reply.", null);
        }
    }

    internal void ValidateExecution(bool sending)
    {
        if (!Started || Request == null)
        {
            throw new FtpProtocolException(
                "A clear FTP data transfer has not started.", null);
        }

        if (Sending != sending)
        {
            throw new FtpProtocolException(
                "The clear FTP data transfer direction does not match its begin operation.",
                null);
        }

        if (DataComplete)
        {
            throw new FtpProtocolException(
                "The clear FTP data transfer already completed.", null);
        }
    }

    internal static void ValidateRequest(
        FtpClearDataStreamRequest? request,
        bool sending)
    {
        if (request == null || request.LocalStream == null || request.Options == null)
        {
            throw new FtpConfigurationException(
                "A local stream and transfer options are required.", null);
        }

        if (!FtpReplyCoordinator.IsDataCommand(request.CommandKind))
        {
            throw new FtpConfigurationException(
                "Only LIST, NLST, MLSD, RETR, STOR, STOU, and APPE use clear data streams.",
                null);
        }

        FtpTransferValidator.ValidateTransferOptions(request.Options);
        if (sending && !request.LocalStream.CanRead)
        {
            throw new FtpConfigurationException(
                "A readable local stream is required for data sending.", null);
        }

        if (!sending && !request.LocalStream.CanWrite)
        {
            throw new FtpConfigurationException(
                "A writable local stream is required for data receiving.", null);
        }

        if (request.ConnectionTimeoutMilliseconds < Timeout.Infinite ||
            request.TransferTimeoutMilliseconds < Timeout.Infinite)
        {
            throw new FtpConfigurationException(
                "Data connection and transfer timeouts must be non-negative or infinite.",
                null);
        }

        if (request.ExpectedBytes < 0 ||
            (!request.HasExpectedBytes && request.ExpectedBytes != 0) ||
            request.InitialLogicalBytes < 0 || request.InitialWireBytes < 0)
        {
            throw new FtpConfigurationException(
                "Clear data-stream byte accounting is invalid.", null);
        }

        if ((request.HasMaximumBytes && request.MaximumBytes < 0) ||
            (!request.HasMaximumBytes && request.MaximumBytes != 0) ||
            (request.HasDestinationMaximumBytes &&
                request.DestinationMaximumBytes < 0) ||
            (!request.HasDestinationMaximumBytes &&
                request.DestinationMaximumBytes != 0) ||
            (request.HasBytesPerSecond && request.BytesPerSecond <= 0) ||
            (!request.HasBytesPerSecond && request.BytesPerSecond != 0))
        {
            throw new FtpConfigurationException(
                "Clear data-stream resource limits are invalid.", null);
        }

        if (request.Direction != FtpTransferDirection.Download &&
            request.Direction != FtpTransferDirection.Upload &&
            request.Direction != FtpTransferDirection.ServerToServer)
        {
            throw new FtpConfigurationException(
                "The clear data-stream transfer direction is invalid.", null);
        }
    }

    internal FtpClearDataStreamRequest RequireRequest()
    {
        if (Request == null)
        {
            throw new FtpProtocolException(
                "A clear data-stream request is unavailable.", null);
        }

        return Request;
    }

    internal void ThrowTransferFailure(
        FtpFaultIsolationResult transferResult,
        FtpFaultIsolationResult modeZCleanup,
        FtpFaultIsolationResult streamShutdown,
        FtpFaultIsolationResult streamCleanup,
        FtpFaultIsolationResult abortResult,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The clear FTP data transfer was canceled.",
                transferResult.Failure,
                cancellationToken);
        }

        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException(
                "The clear FTP data transfer timed out.", transferResult.Failure);
        }

        if (!transferResult.Succeeded)
        {
            throw new FtpTransferException(
                "The clear FTP data transfer failed.", transferResult.Failure);
        }

        if (!modeZCleanup.Succeeded)
        {
            throw new FtpTransferException(
                "The MODE Z data stream could not be finalized.",
                modeZCleanup.Failure);
        }

        if (!streamShutdown.Succeeded)
        {
            throw new FtpTransferException(
                "The protected FTP data stream could not shut down cleanly.",
                streamShutdown.Failure);
        }

        if (!streamCleanup.Succeeded)
        {
            throw new FtpTransferException(
                "The clear FTP data stream could not be closed.", streamCleanup.Failure);
        }

        if (!abortResult.Succeeded)
        {
            throw new FtpConnectionException(
                "The failed clear FTP data connection could not be aborted.",
                abortResult.Failure);
        }
    }

    internal static FtpFaultIsolationResult CreateSuccessfulResult()
    {
        FtpFaultIsolationResult result = new FtpFaultIsolationResult();
        result.Succeeded = true;
        return result;
    }
}
