using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace tftp;

internal class TftpClientReadStateMachine
{
    internal TftpReadRequest Request;
    internal TftpTransferOptions TransferOptions;
    internal Stream Destination;
    internal IPEndPoint InitialServerEndPoint;
    internal TftpPeerTracker PeerTracker;
    internal TftpRetransmissionTimer RetransmissionTimer;
    internal TftpPacket? PendingRequestPacket;
    internal List<TftpOption> ActiveRequestedOptions = [];
    internal List<TftpOption> NegotiatedOptions = [];
    internal int EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
    internal int EffectiveTimeoutSeconds = TftpProtocolValues.MinimumTimeoutSeconds;
    internal bool HasTransferSize = false;
    internal long TransferSize = 0;
    internal bool Started = false;
    internal bool EnteredReceiver = false;
    internal bool RetriedWithoutOptions = false;
    internal bool Completed = false;
    internal bool Failed = false;
    internal bool Cancelled = false;
    internal TftpReceiverStateMachine? Receiver = null;

    internal TftpClientReadStateMachine(
        TftpReadRequest request,
        Stream destination,
        IPEndPoint serverEndPoint,
        ITftpTimeSource timeSource)
    {
        if (request == null)
        {
            throw new TftpProtocolException(
                "The client read request cannot be null.",
                false,
                0);
        }

        TftpTransferOptions? transferOptions = request.Options;
        if (transferOptions == null)
        {
            throw new TftpProtocolException(
                "The client read transfer options cannot be null.",
                false,
                0);
        }

        if (destination == null)
        {
            throw new TftpProtocolException(
                "The client read destination stream cannot be null.",
                false,
                0);
        }

        if (!destination.CanWrite)
        {
            throw new TftpProtocolException(
                "The client read destination stream must be writable.",
                false,
                0);
        }

        if (serverEndPoint == null ||
            serverEndPoint.Port < 1 ||
            serverEndPoint.Port > 65535)
        {
            throw new TftpProtocolException(
                "The client read server endpoint is invalid.",
                false,
                0);
        }

        if (transferOptions.MaximumTransferSize < 0)
        {
            throw new TftpProtocolException(
                "The client read maximum transfer size cannot be negative.",
                false,
                0);
        }

        if (transferOptions.DallyDurationSeconds < 0)
        {
            throw new TftpProtocolException(
                "The client read dally duration cannot be negative.",
                false,
                0);
        }

        string modeName = "";
        if (transferOptions.Mode == TftpTransferMode.Octet)
        {
            modeName = TftpProtocolValues.OctetMode;
        }
        else if (transferOptions.Mode == TftpTransferMode.Netascii)
        {
            modeName = TftpProtocolValues.NetasciiMode;
        }
        else
        {
            throw new TftpProtocolException(
                "The client read transfer mode is invalid.",
                false,
                0);
        }

        TftpPacket requestPacket = new TftpPacket();
        requestPacket.Opcode = TftpOpcode.ReadRequest;
        requestPacket.Filename = request.RemoteFilename;
        requestPacket.Mode = modeName;
        TftpPacketCodec codec = new TftpPacketCodec();
        int requestBaseLength = codec.GetEncodedLength(requestPacket);
        TftpOptionNegotiationResult requestOptions =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                transferOptions,
                TftpTransferDirection.Read,
                false,
                0,
                requestBaseLength);
        if (!requestOptions.Success)
        {
            throw new TftpOptionNegotiationException(
                requestOptions.FailureMessage,
                requestOptions.OptionName,
                requestOptions.OptionValue);
        }

        requestPacket.Options = requestOptions.Options;
        codec.GetEncodedLength(requestPacket);
        IPEndPoint endpointCopy = new IPEndPoint(
            serverEndPoint.Address,
            serverEndPoint.Port);
        Request = request;
        TransferOptions = transferOptions;
        Destination = destination;
        InitialServerEndPoint = endpointCopy;
        PeerTracker = new TftpPeerTracker(endpointCopy.Address, 0, false);
        RetransmissionTimer = new TftpRetransmissionTimer(
            timeSource,
            transferOptions.InitialTimeoutSeconds,
            transferOptions.MaximumTimeoutSeconds,
            transferOptions.MaximumRetries,
            transferOptions.MaximumTransferDurationSeconds,
            false,
            0);
        PendingRequestPacket = requestPacket;
        ActiveRequestedOptions = requestOptions.Options;
        EffectiveTimeoutSeconds = transferOptions.InitialTimeoutSeconds;
    }

    internal TftpClientReadResult StartRead(CancellationToken cancellationToken)
    {
        ThrowForReadCancellation(cancellationToken);
        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The client read transfer has already been cancelled.",
                false,
                0);
        }

        if (Started)
        {
            throw new TftpProtocolException(
                "The client read state machine has already started.",
                false,
                0);
        }

        TftpPacket? requestPacket = PendingRequestPacket;
        if (requestPacket == null)
        {
            throw new TftpProtocolException(
                "The client read request packet is unavailable.",
                false,
                0);
        }

        Started = true;
        RetransmissionTimer.RestartCurrentInterval();
        return CreateReadResult(
            TftpClientReadAction.SendRequest,
            requestPacket,
            InitialServerEndPoint,
            0);
    }

    internal TftpClientReadResult ProcessReadPacket(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        ThrowForReadCancellation(cancellationToken);
        EnsureActiveRead();
        TftpPeerValidation peerValidation =
            PeerTracker.ClassifyPeer(peerEndPoint);
        if (peerValidation == TftpPeerValidation.UnexpectedAddress)
        {
            return CreateReadResult(
                TftpClientReadAction.IgnoreUnexpectedPeer,
                null,
                null,
                0);
        }

        if (peerValidation == TftpPeerValidation.UnknownTransferId)
        {
            TftpPacket unknownTransferId = CreateReadErrorPacket(
                TftpErrorCode.UnknownTransferId,
                "Unknown transfer ID.");
            return CreateReadResult(
                TftpClientReadAction.RejectUnknownTransferId,
                unknownTransferId,
                peerEndPoint,
                0);
        }

        if (packet == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The client read response packet cannot be null.",
                false,
                0);
        }

        if (EnteredReceiver)
        {
            return ProcessReceiverPacket(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        if (packet.Opcode == TftpOpcode.Error)
        {
            return ProcessInitialError(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        FailReadIfTotalDurationReached();
        if (packet.Opcode == TftpOpcode.OptionAcknowledgement)
        {
            return ProcessInitialOptionAcknowledgement(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        if (packet.Opcode == TftpOpcode.Data)
        {
            return ProcessInitialData(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        return CreateInitialProtocolFailure(
            "The RRQ response must be OACK, DATA block 1, or ERROR.",
            peerEndPoint);
    }

    internal TftpClientReadResult EvaluateReadTimer(
        CancellationToken cancellationToken)
    {
        ThrowForReadCancellation(cancellationToken);
        EnsureActiveRead();
        if (EnteredReceiver)
        {
            TftpReceiverStateMachine? receiver = Receiver;
            if (receiver == null)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The client read receiver state is unavailable.",
                    false,
                    0);
            }

            TftpReceiverResult receiverResult =
                receiver.EvaluateReceiverTimer(cancellationToken);
            return MapReceiverResult(receiverResult);
        }

        TftpRetransmissionResult timerResult =
            RetransmissionTimer.EvaluateTimer();
        if (timerResult.Action == TftpRetransmissionAction.Wait)
        {
            return CreateReadResult(
                TftpClientReadAction.Wait,
                null,
                null,
                timerResult.WaitMilliseconds);
        }

        if (timerResult.Action == TftpRetransmissionAction.Retransmit)
        {
            TftpPacket? requestPacket = PendingRequestPacket;
            if (requestPacket == null)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The client read request is unavailable for retransmission.",
                    false,
                    0);
            }

            return CreateReadResult(
                TftpClientReadAction.SendRequest,
                requestPacket,
                InitialServerEndPoint,
                0);
        }

        Failed = true;
        throw RetransmissionTimer.CreateTimeoutFailure(timerResult);
    }

    internal TftpClientReadResult ProcessInitialError(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        bool canRetry = TftpOptionNegotiator.CanRetryRequestWithoutOptions(
            TransferOptions,
            ActiveRequestedOptions,
            RetriedWithoutOptions,
            packet.ErrorCode);
        if (canRetry)
        {
            FailReadIfTotalDurationReached();
            TftpPacket optionlessRequest = CreateOptionlessReadRequest();
            ActiveRequestedOptions = [];
            PendingRequestPacket = optionlessRequest;
            RetriedWithoutOptions = true;
            RetransmissionTimer.RecordValidProgress();
            ThrowForReadCancellation(cancellationToken);
            return CreateReadResult(
                TftpClientReadAction.SendRequest,
                optionlessRequest,
                InitialServerEndPoint,
                0);
        }

        PeerTracker.LockTransferEndpoint(peerEndPoint);
        Failed = true;
        string remoteMessage = packet.ErrorMessage;
        if (remoteMessage == null)
        {
            remoteMessage = "";
        }

        throw new TftpRemoteErrorException(
            packet.ErrorCode,
            remoteMessage);
    }

    internal TftpClientReadResult ProcessInitialOptionAcknowledgement(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        TftpOptionNegotiationResult negotiationResult;
        List<TftpOption>? acknowledgedOptions = packet.Options;
        if (acknowledgedOptions == null)
        {
            negotiationResult = TftpOptionNegotiator.CreateOptionFailure(
                "The OACK option list cannot be null.",
                "",
                "");
        }
        else
        {
            negotiationResult =
                TftpOptionNegotiator.ValidateCombinedAcknowledgedOptions(
                    ActiveRequestedOptions,
                    acknowledgedOptions,
                    TftpTransferDirection.Read,
                    TransferOptions.InitialTimeoutSeconds,
                    TransferOptions.MaximumTransferSize);
        }

        if (!negotiationResult.Success)
        {
            PeerTracker.LockTransferEndpoint(peerEndPoint);
            Failed = true;
            TftpPacket failurePacket = CreateReadErrorPacket(
                negotiationResult.ErrorCode,
                negotiationResult.FailureMessage);
            TftpClientReadResult failureResult = CreateReadResult(
                TftpClientReadAction.SendError,
                failurePacket,
                peerEndPoint,
                0);
            failureResult.HasFailure = true;
            failureResult.IsOptionFailure = true;
            failureResult.FailureErrorCode = negotiationResult.ErrorCode;
            failureResult.FailureMessage = negotiationResult.FailureMessage;
            failureResult.FailureOptionName = negotiationResult.OptionName;
            failureResult.FailureOptionValue = negotiationResult.OptionValue;
            return failureResult;
        }

        PeerTracker.LockTransferEndpoint(peerEndPoint);
        NegotiatedOptions = negotiationResult.Options;
        EffectiveBlockSize = negotiationResult.EffectiveBlockSize;
        EffectiveTimeoutSeconds = negotiationResult.EffectiveTimeoutSeconds;
        HasTransferSize = negotiationResult.HasTransferSize;
        TransferSize = negotiationResult.TransferSize;
        if (HasNegotiatedReadOption(TftpProtocolValues.TimeoutOption))
        {
            RetransmissionTimer.ApplyNegotiatedTimeout(
                EffectiveTimeoutSeconds);
        }
        else
        {
            RetransmissionTimer.RecordValidProgress();
        }

        TftpPacket acknowledgement = new TftpPacket();
        acknowledgement.Opcode = TftpOpcode.Acknowledgement;
        acknowledgement.BlockNumber = 0;
        TftpReceiverStateMachine receiver = CreateReadReceiver();
        Receiver = receiver;
        EnteredReceiver = true;
        receiver.StartReceiving(acknowledgement, cancellationToken);
        PendingRequestPacket = null;
        return CreateReadResult(
            TftpClientReadAction.SendAcknowledgement,
            acknowledgement,
            peerEndPoint,
            0);
    }

    internal TftpClientReadResult ProcessInitialData(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        byte[]? payload = packet.Payload;
        if (packet.BlockNumber != 1 ||
            payload == null ||
            payload.Length > TftpProtocolValues.DefaultBlockSize)
        {
            return CreateInitialProtocolFailure(
                "An optionless RRQ response must begin with DATA block 1 at the default block size.",
                peerEndPoint);
        }

        PeerTracker.LockTransferEndpoint(peerEndPoint);
        TftpOptionNegotiationResult fallback =
            TftpOptionNegotiator.CreateLegacyTimeoutFallback(
                TransferOptions.InitialTimeoutSeconds);
        NegotiatedOptions = fallback.Options;
        EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
        EffectiveTimeoutSeconds = fallback.EffectiveTimeoutSeconds;
        HasTransferSize = false;
        TransferSize = 0;
        TftpPacket? retainedRequest = PendingRequestPacket;
        if (retainedRequest == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The client read request is unavailable for receiver entry.",
                false,
                0);
        }

        TftpReceiverStateMachine receiver = CreateReadReceiver();
        Receiver = receiver;
        EnteredReceiver = true;
        receiver.StartReceiving(retainedRequest, cancellationToken);
        PendingRequestPacket = null;
        TftpReceiverResult receiverResult = receiver.ProcessDataPacket(
            packet,
            cancellationToken);
        return MapReceiverResult(receiverResult);
    }

    internal TftpClientReadResult ProcessReceiverPacket(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        TftpReceiverStateMachine? receiver = Receiver;
        if (receiver == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The client read receiver state is unavailable.",
                false,
                0);
        }

        TftpPacket? retainedPacket = receiver.RetainedPacket;
        if (packet.Opcode == TftpOpcode.OptionAcknowledgement &&
            retainedPacket != null &&
            retainedPacket.Opcode == TftpOpcode.Acknowledgement &&
            retainedPacket.BlockNumber == 0 &&
            receiver.BlockCounter.CurrentBlock == 1 &&
            !receiver.BlockCounter.HasPreviousBlock &&
            IsMatchingReadNegotiation(packet))
        {
            receiver.DuplicatePacketCount =
                receiver.DuplicatePacketCount + 1;
            receiver.RetransmissionTimer.RestartCurrentInterval();
            TftpReceiverResult duplicateResult =
                receiver.CreateReceiverResult(
                    TftpReceiverAction.ReacknowledgeDuplicateData,
                    retainedPacket,
                    0,
                    false);
            return MapReceiverResult(duplicateResult);
        }

        if (packet.Opcode == TftpOpcode.Error)
        {
            Failed = true;
        }

        TftpReceiverResult receiverResult = receiver.ProcessDataPacket(
            packet,
            cancellationToken);
        return MapReceiverResult(receiverResult);
    }

    internal TftpClientReadResult MapReceiverResult(
        TftpReceiverResult receiverResult)
    {
        TftpClientReadAction action = TftpClientReadAction.Wait;
        if (receiverResult.Action == TftpReceiverAction.SendAcknowledgement)
        {
            action = TftpClientReadAction.SendAcknowledgement;
        }
        else if (receiverResult.Action ==
            TftpReceiverAction.ReacknowledgeDuplicateData)
        {
            action = TftpClientReadAction.ReacknowledgeData;
        }
        else if (receiverResult.Action == TftpReceiverAction.RetransmitPacket)
        {
            action = TftpClientReadAction.RetransmitPacket;
        }
        else if (receiverResult.Action == TftpReceiverAction.Completed)
        {
            action = TftpClientReadAction.Completed;
            Completed = true;
        }

        IPEndPoint? targetEndPoint = null;
        if (receiverResult.Packet != null)
        {
            targetEndPoint = new IPEndPoint(
                PeerTracker.ExpectedAddress,
                PeerTracker.TransferPort);
        }

        TftpClientReadResult result = CreateReadResult(
            action,
            receiverResult.Packet,
            targetEndPoint,
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

    internal TftpClientReadResult CreateInitialProtocolFailure(
        string message,
        IPEndPoint peerEndPoint)
    {
        PeerTracker.LockTransferEndpoint(peerEndPoint);
        Failed = true;
        TftpPacket failurePacket = CreateReadErrorPacket(
            TftpErrorCode.IllegalOperation,
            message);
        TftpClientReadResult result = CreateReadResult(
            TftpClientReadAction.SendError,
            failurePacket,
            peerEndPoint,
            0);
        result.HasFailure = true;
        result.FailureErrorCode = TftpErrorCode.IllegalOperation;
        result.FailureMessage = message;
        return result;
    }

    internal TftpReceiverStateMachine CreateReadReceiver()
    {
        return new TftpReceiverStateMachine(
            Destination,
            TransferOptions.Mode,
            EffectiveBlockSize,
            TransferOptions.MaximumTransferSize,
            RetransmissionTimer,
            TransferOptions.DallyDurationSeconds);
    }

    internal TftpPacket CreateOptionlessReadRequest()
    {
        TftpPacket requestPacket = new TftpPacket();
        requestPacket.Opcode = TftpOpcode.ReadRequest;
        requestPacket.Filename = Request.RemoteFilename;
        requestPacket.Mode = PendingReadModeName();
        return requestPacket;
    }

    internal string PendingReadModeName()
    {
        if (TransferOptions.Mode == TftpTransferMode.Netascii)
        {
            return TftpProtocolValues.NetasciiMode;
        }

        return TftpProtocolValues.OctetMode;
    }

    internal bool HasNegotiatedReadOption(string optionName)
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

    internal bool IsMatchingReadNegotiation(TftpPacket packet)
    {
        List<TftpOption>? options = packet.Options;
        if (options == null || options.Count != NegotiatedOptions.Count)
        {
            return false;
        }

        int index = 0;
        while (index < options.Count)
        {
            TftpOption option = options[index];
            TftpOption negotiatedOption = NegotiatedOptions[index];
            if (!string.Equals(
                    option.Name,
                    negotiatedOption.Name,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    option.Value,
                    negotiatedOption.Value,
                    StringComparison.Ordinal))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal TftpPacket CreateReadErrorPacket(
        TftpErrorCode errorCode,
        string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message;
        return packet;
    }

    internal void FailReadIfTotalDurationReached()
    {
        long now = RetransmissionTimer.TimeSource.GetTimestampMilliseconds();
        long elapsed = RetransmissionTimer.GetNonnegativeElapsed(
            RetransmissionTimer.TransferStartedMilliseconds,
            now);
        if (elapsed >= RetransmissionTimer.MaximumDurationMilliseconds)
        {
            Failed = true;
            TftpRetransmissionResult totalFailure =
                RetransmissionTimer.CreateTimerResult(
                    TftpRetransmissionAction.TotalDurationReached,
                    0,
                    elapsed);
            throw RetransmissionTimer.CreateTimeoutFailure(totalFailure);
        }
    }

    internal void EnsureActiveRead()
    {
        if (!Started)
        {
            throw new TftpProtocolException(
                "The client read state machine has not started.",
                false,
                0);
        }

        if (Completed)
        {
            throw new TftpProtocolException(
                "The client read transfer has already completed.",
                false,
                0);
        }

        if (Failed)
        {
            throw new TftpProtocolException(
                "The client read transfer has already failed.",
                false,
                0);
        }

        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The client read transfer has already been cancelled.",
                false,
                0);
        }
    }

    internal void ThrowForReadCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Cancelled = true;
            throw new OperationCanceledException(cancellationToken);
        }
    }

    internal TftpClientReadResult CreateReadResult(
        TftpClientReadAction action,
        TftpPacket? packet,
        IPEndPoint? targetEndPoint,
        long waitMilliseconds)
    {
        TftpClientReadResult result = new TftpClientReadResult();
        result.Action = action;
        result.Packet = packet;
        result.TargetEndPoint = targetEndPoint;
        result.WaitMilliseconds = waitMilliseconds;
        result.EnteredReceiver = EnteredReceiver;
        result.RetriedWithoutOptions = RetriedWithoutOptions;
        result.EffectiveBlockSize = EffectiveBlockSize;
        result.EffectiveTimeoutSeconds = EffectiveTimeoutSeconds;
        result.HasTransferSize = HasTransferSize;
        result.TransferSize = TransferSize;
        result.NegotiatedOptions = NegotiatedOptions;
        result.RetransmissionCount =
            RetransmissionTimer.TotalRetransmissionCount;
        return result;
    }
}
