using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace tftp;

internal class TftpServerWriteStateMachine : IDisposable
{
    internal TftpPacket RequestPacket;
    internal TftpServerRequest ServerRequest;
    internal TftpServerStorageFlow StorageFlow;
    internal TftpServerOptions ServerOptions;
    internal ITftpTimeSource TimeSource;
    internal Stream? PendingContent;
    internal bool OwnsPendingContent;
    internal object? CompletionToken;
    internal bool PendingOpened;
    internal bool PendingResolved;
    internal bool StorageCommitted;
    internal TftpRetransmissionTimer? RetransmissionTimer;
    internal TftpReceiverStateMachine? Receiver;
    internal List<TftpOption> NegotiatedOptions;
    internal int EffectiveBlockSize;
    internal int EffectiveTimeoutSeconds;
    internal bool HasTransferSize;
    internal long TransferSize;
    internal bool Started;
    internal bool Completed;
    internal bool Failed;
    internal bool Cancelled;

    internal TftpServerWriteStateMachine(
        TftpPacket requestPacket,
        IPEndPoint clientEndPoint,
        ITftpServerStorage storage,
        TftpServerOptions serverOptions,
        ITftpTimeSource timeSource)
    {
        if (requestPacket == null || clientEndPoint == null ||
            serverOptions == null || timeSource == null)
        {
            throw new TftpProtocolException(
                "The server write state inputs cannot be null.",
                false,
                0);
        }

        TftpServerReadStateMachine.ValidateServerOptions(serverOptions);
        if (serverOptions.DallyDurationSeconds < 0)
        {
            throw new TftpProtocolException(
                "The server dally duration cannot be negative.",
                false,
                0);
        }

        RequestPacket = TftpServerSession.CopyPacket(requestPacket);
        ServerRequest = TftpServerSession.CreateServerRequest(
            RequestPacket,
            clientEndPoint);
        StorageFlow = new TftpServerStorageFlow(storage);
        ServerOptions = TftpServerReadStateMachine.CopyServerOptions(
            serverOptions);
        TimeSource = timeSource;
        PendingContent = null;
        OwnsPendingContent = false;
        CompletionToken = null;
        PendingOpened = false;
        PendingResolved = false;
        StorageCommitted = false;
        RetransmissionTimer = null;
        Receiver = null;
        NegotiatedOptions = [];
        EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
        EffectiveTimeoutSeconds = serverOptions.InitialTimeoutSeconds;
        HasTransferSize = false;
        TransferSize = 0;
        Started = false;
        Completed = false;
        Failed = false;
        Cancelled = false;
    }

    internal TftpServerWriteResult StartWrite(
        CancellationToken cancellationToken)
    {
        ThrowForWriteCancellation(cancellationToken);
        if (Started)
        {
            throw new TftpProtocolException(
                "The server write state machine has already started.",
                false,
                0);
        }

        Started = true;
        string requestFailure = ValidateWriteRequest();
        if (requestFailure.Length != 0)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                requestFailure,
                false);
        }

        int requestBaseLength = TftpProtocolValues.OpcodeLength +
            RequestPacket.Filename.Length + 1 + RequestPacket.Mode.Length + 1;
        if (requestBaseLength > ServerOptions.MaximumRequestPacketLength ||
            requestBaseLength > TftpProtocolValues.MaximumRequestPacketLength)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The WRQ packet exceeds the configured request length limit.",
                false);
        }

        TftpOptionNegotiationResult negotiation =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                RequestPacket.Options,
                ServerOptions,
                requestBaseLength,
                TftpTransferDirection.Write,
                false,
                0);
        if (!negotiation.Success)
        {
            return CreateOptionWriteFailure(negotiation);
        }

        if (negotiation.EncodedLength >
            ServerOptions.MaximumRequestPacketLength)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The WRQ packet exceeds the configured request length limit.",
                false);
        }

        NegotiatedOptions =
            TftpServerSession.CopyOptions(negotiation.Options);
        EffectiveBlockSize = negotiation.EffectiveBlockSize;
        EffectiveTimeoutSeconds = negotiation.EffectiveTimeoutSeconds;
        HasTransferSize = negotiation.HasTransferSize;
        TransferSize = negotiation.TransferSize;
        ServerRequest.RequestedOptions =
            TftpServerSession.CopyOptions(RequestPacket.Options);
        ServerRequest.EffectiveOptions =
            TftpServerSession.CopyOptions(negotiation.Options);
        ServerRequest.HasAnnouncedSize = HasTransferSize;
        ServerRequest.AnnouncedSize = TransferSize;
        TftpStorageOpenResult openResult = StorageFlow.OpenPendingWrite(
            ServerRequest,
            cancellationToken);
        if (!openResult.Success)
        {
            return CreateWriteFailure(
                openResult.ErrorCode,
                openResult.ErrorMessage,
                false);
        }

        Stream? openedContent = openResult.Content;
        object? completionToken = openResult.CompletionToken;
        if (openedContent == null || completionToken == null)
        {
            throw new TftpStorageException(
                "The validated pending-write result is incomplete.",
                "OpenPendingWrite",
                null);
        }

        PendingContent = openedContent;
        OwnsPendingContent = openResult.OwnsContent;
        CompletionToken = completionToken;
        PendingOpened = true;
        ThrowForWriteCancellation(cancellationToken);
        bool hasNegotiatedTimeout = HasNegotiatedOption(
            TftpProtocolValues.TimeoutOption);
        TftpRetransmissionTimer timer = new TftpRetransmissionTimer(
            TimeSource,
            ServerOptions.InitialTimeoutSeconds,
            ServerOptions.MaximumTimeoutSeconds,
            ServerOptions.MaximumRetries,
            ServerOptions.MaximumSessionDurationSeconds,
            hasNegotiatedTimeout,
            EffectiveTimeoutSeconds);
        RetransmissionTimer = timer;
        TftpReceiverStateMachine receiver = new TftpReceiverStateMachine(
            openedContent,
            ServerRequest.Mode,
            EffectiveBlockSize,
            ServerOptions.MaximumTransferSize,
            timer,
            ServerOptions.DallyDurationSeconds);
        Receiver = receiver;
        TftpPacket initialResponse = new TftpPacket();
        TftpServerWriteAction initialAction =
            TftpServerWriteAction.SendAcknowledgement;
        if (NegotiatedOptions.Count > 0)
        {
            initialResponse.Opcode = TftpOpcode.OptionAcknowledgement;
            initialResponse.Options =
                TftpServerSession.CopyOptions(NegotiatedOptions);
            initialAction = TftpServerWriteAction.SendOptionAcknowledgement;
        }
        else
        {
            initialResponse.Opcode = TftpOpcode.Acknowledgement;
            initialResponse.BlockNumber = 0;
        }

        receiver.StartReceiving(initialResponse, cancellationToken);
        return CreateWriteResult(initialAction, initialResponse, 0);
    }

    internal TftpServerWriteResult ProcessWritePacket(
        TftpPacket packet,
        CancellationToken cancellationToken)
    {
        ThrowForWriteCancellation(cancellationToken);
        EnsureActiveWrite();
        if (packet == null)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The server write received a null packet.",
                false);
        }

        FailIfTotalDurationReached();
        if (packet.Opcode == TftpOpcode.Error)
        {
            Failed = true;
            AbortPendingWrite();
            string remoteMessage = packet.ErrorMessage;
            if (remoteMessage == null)
            {
                remoteMessage = "";
            }

            throw new TftpRemoteErrorException(
                packet.ErrorCode,
                remoteMessage);
        }

        if (packet.Opcode != TftpOpcode.Data || packet.Payload == null)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The server write expected a DATA packet.",
                false);
        }

        TftpReceiverStateMachine receiver = AvailableReceiver();
        if (packet.Payload.Length > EffectiveBlockSize)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The received DATA payload exceeds the active block size.",
                false);
        }

        if (!receiver.Dallying)
        {
            TftpBlockClassification classification =
                receiver.BlockCounter.ClassifyDataBlock(packet.BlockNumber);
            if (classification != TftpBlockClassification.Expected &&
                classification != TftpBlockClassification.Previous)
            {
                return CreateWriteFailure(
                    TftpErrorCode.IllegalOperation,
                    "The server write received an unexpected DATA block number.",
                    false);
            }

            if (classification == TftpBlockClassification.Expected)
            {
                bool isFinalData = packet.Payload.Length < EffectiveBlockSize;
                TftpServerWritePayloadValidation payloadValidation =
                    ValidateExpectedPayload(packet.Payload, isFinalData);
                if (!payloadValidation.Success)
                {
                    return CreateWriteFailure(
                        payloadValidation.ErrorCode,
                        payloadValidation.FailureMessage,
                        false);
                }
            }
        }
        else if (packet.BlockNumber != receiver.FinalBlockNumber ||
            packet.Payload.Length != receiver.FinalDataLength)
        {
            return CreateWriteFailure(
                TftpErrorCode.IllegalOperation,
                "The server write received unexpected DATA while dallying.",
                false);
        }

        TftpReceiverResult receiverResult = receiver.ProcessDataPacket(
            packet,
            cancellationToken);
        return MapReceiverResult(receiverResult);
    }

    internal TftpServerWriteResult EvaluateWriteTimer(
        CancellationToken cancellationToken)
    {
        ThrowForWriteCancellation(cancellationToken);
        EnsureActiveWrite();
        TftpReceiverStateMachine receiver = AvailableReceiver();
        TftpRetransmissionTimer timer = AvailableTimer();
        if (receiver.Dallying)
        {
            FailIfTotalDurationReached();
            TftpReceiverResult dallyResult =
                receiver.EvaluateDallyTimer(cancellationToken);
            return MapReceiverResult(dallyResult);
        }

        TftpRetransmissionResult timerResult = timer.EvaluateTimer();
        if (timerResult.Action == TftpRetransmissionAction.Wait)
        {
            return CreateWriteResult(
                TftpServerWriteAction.Wait,
                null,
                timerResult.WaitMilliseconds);
        }

        if (timerResult.Action == TftpRetransmissionAction.Retransmit)
        {
            TftpPacket? retainedPacket = receiver.RetainedPacket;
            if (retainedPacket == null)
            {
                Failed = true;
                AbortPendingWrite();
                throw new TftpProtocolException(
                    "The server write has no packet available for retransmission.",
                    false,
                    0);
            }

            return CreateWriteResult(
                TftpServerWriteAction.RetransmitPacket,
                retainedPacket,
                0);
        }

        Failed = true;
        AbortPendingWrite();
        throw timer.CreateTimeoutFailure(timerResult);
    }

    internal TftpServerWriteResult MapReceiverResult(
        TftpReceiverResult receiverResult)
    {
        TftpServerWriteAction action = TftpServerWriteAction.Wait;
        if (receiverResult.Action == TftpReceiverAction.SendAcknowledgement)
        {
            action = TftpServerWriteAction.SendAcknowledgement;
        }
        else if (receiverResult.Action ==
            TftpReceiverAction.ReacknowledgeDuplicateData)
        {
            action = TftpServerWriteAction.ReacknowledgeData;
        }
        else if (receiverResult.Action ==
            TftpReceiverAction.RetransmitPacket)
        {
            action = TftpServerWriteAction.RetransmitPacket;
        }
        else if (receiverResult.Action == TftpReceiverAction.Completed)
        {
            return CommitPendingWrite();
        }

        TftpServerWriteResult result = CreateWriteResult(
            action,
            receiverResult.Packet,
            receiverResult.WaitMilliseconds);
        result.IsFinalData = receiverResult.IsFinalData;
        result.IsDallying = receiverResult.IsDallying;
        result.BytesTransferred = receiverResult.BytesTransferred;
        result.EncodedBytesTransferred =
            receiverResult.EncodedBytesTransferred;
        result.RetransmissionCount = receiverResult.RetransmissionCount;
        result.DuplicatePacketCount = receiverResult.DuplicatePacketCount;
        return result;
    }

    internal TftpServerWritePayloadValidation ValidateExpectedPayload(
        byte[] payload,
        bool isFinalData)
    {
        TftpServerWritePayloadValidation validation =
            new TftpServerWritePayloadValidation();
        TftpReceiverStateMachine receiver = AvailableReceiver();
        if (payload.Length >
            ServerOptions.MaximumTransferSize -
                receiver.EncodedBytesTransferred)
        {
            validation.ErrorCode = TftpErrorCode.StorageExhausted;
            validation.FailureMessage =
                "The received content exceeds the maximum transfer size.";
            return validation;
        }

        long encodedTotal = receiver.EncodedBytesTransferred + payload.Length;
        if (HasTransferSize && encodedTotal > TransferSize)
        {
            validation.ErrorCode = TftpErrorCode.IllegalOperation;
            validation.FailureMessage =
                "The received content exceeds the announced transfer size.";
            return validation;
        }

        if (HasTransferSize && isFinalData && encodedTotal != TransferSize)
        {
            validation.ErrorCode = TftpErrorCode.IllegalOperation;
            validation.FailureMessage =
                "The received content does not match the announced transfer size.";
            return validation;
        }

        if (ServerRequest.Mode == TftpTransferMode.Netascii)
        {
            bool pendingCarriageReturn =
                receiver.Decoder.HasPendingCarriageReturn;
            long decodedIncrement = 0;
            int payloadIndex = 0;
            while (payloadIndex < payload.Length)
            {
                byte value = payload[payloadIndex];
                if (pendingCarriageReturn)
                {
                    if (value != NetasciiDecoder.LineFeed &&
                        value != NetasciiDecoder.Null)
                    {
                        validation.ErrorCode = TftpErrorCode.IllegalOperation;
                        validation.FailureMessage =
                            "A netascii carriage return must be followed by line feed or NUL.";
                        return validation;
                    }

                    decodedIncrement += 1;
                    pendingCarriageReturn = false;
                }
                else if (value == NetasciiDecoder.CarriageReturn)
                {
                    pendingCarriageReturn = true;
                }
                else
                {
                    decodedIncrement += 1;
                }

                payloadIndex += 1;
            }

            if (isFinalData && pendingCarriageReturn)
            {
                validation.ErrorCode = TftpErrorCode.IllegalOperation;
                validation.FailureMessage =
                    "The netascii stream ends with an incomplete carriage-return sequence.";
                return validation;
            }

            if (decodedIncrement >
                ServerOptions.MaximumTransferSize -
                    receiver.BytesTransferred)
            {
                validation.ErrorCode = TftpErrorCode.StorageExhausted;
                validation.FailureMessage =
                    "The received content exceeds the maximum transfer size.";
                return validation;
            }
        }

        validation.Success = true;
        return validation;
    }

    internal TftpServerWriteResult CommitPendingWrite()
    {
        object? completionToken = CompletionToken;
        if (!PendingOpened || PendingResolved || completionToken == null)
        {
            Failed = true;
            AbortPendingWrite();
            throw new TftpStorageException(
                "The pending write cannot be committed.",
                "CommitWrite",
                null);
        }

        TftpStorageCommitResult commitResult = StorageFlow.CommitWrite(
            completionToken,
            CancellationToken.None);
        if (!commitResult.Success)
        {
            TftpErrorCode errorCode = commitResult.ErrorCode;
            string errorMessage = commitResult.ErrorMessage;
            AbortPendingWrite();
            return CreateWriteFailure(
                errorCode,
                errorMessage,
                false);
        }

        PendingResolved = true;
        StorageCommitted = true;
        CompletionToken = null;
        ClosePendingContent();
        Completed = true;
        return CreateWriteResult(
            TftpServerWriteAction.Completed,
            null,
            0);
    }

    internal void AbortPendingWrite()
    {
        object? completionToken = CompletionToken;
        if (PendingOpened && !PendingResolved && completionToken != null)
        {
            StorageFlow.AbortWrite(
                completionToken,
                CancellationToken.None);
            PendingResolved = true;
            CompletionToken = null;
        }

        ClosePendingContent();
    }

    internal void ClosePendingContent()
    {
        Stream? content = PendingContent;
        PendingContent = null;
        if (content != null && OwnsPendingContent)
        {
            content.Dispose();
        }

        OwnsPendingContent = false;
    }

    internal TftpServerWriteResult CreateWriteResult(
        TftpServerWriteAction action,
        TftpPacket? packet,
        long waitMilliseconds)
    {
        TftpServerWriteResult result = new TftpServerWriteResult();
        result.Action = action;
        result.Packet = packet;
        result.WaitMilliseconds = waitMilliseconds;
        result.StorageCommitted = StorageCommitted;
        result.EffectiveBlockSize = EffectiveBlockSize;
        result.EffectiveTimeoutSeconds = EffectiveTimeoutSeconds;
        result.HasTransferSize = HasTransferSize;
        result.TransferSize = TransferSize;
        result.NegotiatedOptions =
            TftpServerSession.CopyOptions(NegotiatedOptions);
        TftpReceiverStateMachine? receiver = Receiver;
        if (receiver != null)
        {
            result.IsDallying = receiver.Dallying;
            result.BytesTransferred = receiver.BytesTransferred;
            result.EncodedBytesTransferred =
                receiver.EncodedBytesTransferred;
            result.RetransmissionCount =
                AvailableTimer().TotalRetransmissionCount;
            result.DuplicatePacketCount = receiver.DuplicatePacketCount;
        }

        return result;
    }

    internal TftpServerWriteResult CreateWriteFailure(
        TftpErrorCode errorCode,
        string message,
        bool optionFailure)
    {
        Failed = true;
        AbortPendingWrite();
        TftpPacket errorPacket = TftpServerErrorMapper.CreateErrorPacket(
            errorCode,
            message,
            ServerOptions.MaximumRequestPacketLength);

        TftpServerWriteResult result = CreateWriteResult(
            TftpServerWriteAction.SendError,
            errorPacket,
            0);
        result.IsDallying = false;
        result.IsFinalData = false;
        result.HasFailure = true;
        result.IsOptionFailure = optionFailure;
        result.FailureErrorCode = errorPacket.ErrorCode;
        result.FailureMessage = errorPacket.ErrorMessage;
        return result;
    }

    internal TftpServerWriteResult CreateOptionWriteFailure(
        TftpOptionNegotiationResult negotiation)
    {
        TftpServerWriteResult result = CreateWriteFailure(
            negotiation.ErrorCode,
            negotiation.FailureMessage,
            true);
        result.FailureOptionName = negotiation.OptionName;
        result.FailureOptionValue = negotiation.OptionValue;
        return result;
    }

    internal string ValidateWriteRequest()
    {
        if (RequestPacket.Opcode != TftpOpcode.WriteRequest)
        {
            return "The server write requires a WRQ packet.";
        }

        if (RequestPacket.Filename == null ||
            RequestPacket.Filename.Length == 0)
        {
            return "The WRQ filename cannot be empty.";
        }

        if (RequestPacket.Filename.Length >
            ServerOptions.MaximumFilenameLength)
        {
            return "The WRQ filename exceeds the configured length limit.";
        }

        string filenameFailure = ValidateAsciiText(
            RequestPacket.Filename,
            "filename");
        if (filenameFailure.Length != 0)
        {
            return filenameFailure;
        }

        if (RequestPacket.Mode == null || RequestPacket.Mode.Length == 0)
        {
            return "The WRQ mode cannot be empty.";
        }

        if (string.Equals(
            RequestPacket.Mode,
            TftpProtocolValues.OctetMode,
            StringComparison.OrdinalIgnoreCase))
        {
            ServerRequest.Mode = TftpTransferMode.Octet;
        }
        else if (string.Equals(
            RequestPacket.Mode,
            TftpProtocolValues.NetasciiMode,
            StringComparison.OrdinalIgnoreCase))
        {
            ServerRequest.Mode = TftpTransferMode.Netascii;
        }
        else
        {
            return "The WRQ transfer mode is not supported.";
        }

        if (RequestPacket.Options == null)
        {
            return "The WRQ option list cannot be null.";
        }

        ServerRequest.Filename = RequestPacket.Filename;
        ServerRequest.Direction = TftpTransferDirection.Write;
        return "";
    }

    internal string ValidateAsciiText(string value, string fieldName)
    {
        int characterIndex = 0;
        while (characterIndex < value.Length)
        {
            char character = value[characterIndex];
            if (character == 0)
            {
                return "The WRQ " + fieldName +
                    " cannot contain an embedded NUL.";
            }

            if (character > 127)
            {
                return "The WRQ " + fieldName +
                    " must contain only ASCII characters.";
            }

            characterIndex += 1;
        }

        return "";
    }

    internal bool HasNegotiatedOption(string optionName)
    {
        foreach (TftpOption option in NegotiatedOptions)
        {
            if (string.Equals(
                option.Name,
                optionName,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal void FailIfTotalDurationReached()
    {
        TftpRetransmissionTimer timer = AvailableTimer();
        long now = timer.TimeSource.GetTimestampMilliseconds();
        long elapsed = timer.GetNonnegativeElapsed(
            timer.TransferStartedMilliseconds,
            now);
        if (elapsed >= timer.MaximumDurationMilliseconds)
        {
            Failed = true;
            AbortPendingWrite();
            TftpRetransmissionResult totalFailure = timer.CreateTimerResult(
                TftpRetransmissionAction.TotalDurationReached,
                0,
                elapsed);
            throw timer.CreateTimeoutFailure(totalFailure);
        }
    }

    internal void EnsureActiveWrite()
    {
        if (!Started)
        {
            throw new TftpProtocolException(
                "The server write state machine has not started.",
                false,
                0);
        }

        if (Completed)
        {
            throw new TftpProtocolException(
                "The server write transfer has already completed.",
                false,
                0);
        }

        if (Failed)
        {
            throw new TftpProtocolException(
                "The server write transfer has already failed.",
                false,
                0);
        }

        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The server write transfer has already been cancelled.",
                false,
                0);
        }
    }

    internal void ThrowForWriteCancellation(
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Cancelled = true;
            AbortPendingWrite();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    internal TftpReceiverStateMachine AvailableReceiver()
    {
        TftpReceiverStateMachine? receiver = Receiver;
        if (receiver == null)
        {
            throw new TftpProtocolException(
                "The server write receiver state is unavailable.",
                false,
                0);
        }

        return receiver;
    }

    internal TftpRetransmissionTimer AvailableTimer()
    {
        TftpRetransmissionTimer? timer = RetransmissionTimer;
        if (timer == null)
        {
            throw new TftpProtocolException(
                "The server write timer state is unavailable.",
                false,
                0);
        }

        return timer;
    }

    /// <summary>
    /// Aborts pending write content that has not been committed.
    /// </summary>
    public void Dispose()
    {
        AbortPendingWrite();
    }
}
