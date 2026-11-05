using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace tftp;

internal class TftpClientWriteStateMachine
{
    internal TftpWriteRequest Request;
    internal TftpTransferOptions TransferOptions;
    internal Stream Source;
    internal IPEndPoint InitialServerEndPoint;
    internal TftpPeerTracker PeerTracker;
    internal TftpRetransmissionTimer RetransmissionTimer;
    internal TftpPacket? PendingRequestPacket = null;
    internal List<TftpOption> ActiveRequestedOptions = [];
    internal List<TftpOption> NegotiatedOptions = [];
    internal int EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
    internal int EffectiveTimeoutSeconds = TftpProtocolValues.MinimumTimeoutSeconds;
    internal bool HasSourceSize = false;
    internal long SourceSize = 0;
    internal bool HasEncodedSourceSize = false;
    internal long EncodedSourceSize = 0;
    internal bool HasTransferSize = false;
    internal long TransferSize = 0;
    internal bool Started = false;
    internal bool EnteredSender = false;
    internal bool RetriedWithoutOptions = false;
    internal bool Completed = false;
    internal bool Failed = false;
    internal bool Cancelled = false;
    internal TftpSenderStateMachine? Sender = null;

    internal TftpClientWriteStateMachine(
        TftpWriteRequest request,
        Stream source,
        IPEndPoint serverEndPoint,
        ITftpTimeSource timeSource)
    {
        if (request == null)
        {
            throw new TftpProtocolException(
                "The client write request cannot be null.",
                false,
                0);
        }

        TftpTransferOptions? transferOptions = request.Options;
        if (transferOptions == null)
        {
            throw new TftpProtocolException(
                "The client write transfer options cannot be null.",
                false,
                0);
        }

        if (source == null)
        {
            throw new TftpProtocolException(
                "The client write source stream cannot be null.",
                false,
                0);
        }

        if (!source.CanRead)
        {
            throw new TftpProtocolException(
                "The client write source stream must be readable.",
                false,
                0);
        }

        if (serverEndPoint == null ||
            serverEndPoint.Port < 1 ||
            serverEndPoint.Port > 65535)
        {
            throw new TftpProtocolException(
                "The client write server endpoint is invalid.",
                false,
                0);
        }

        if (transferOptions.MaximumTransferSize < 0)
        {
            throw new TftpProtocolException(
                "The client write maximum transfer size cannot be negative.",
                false,
                0);
        }

        if (transferOptions.Mode != TftpTransferMode.Octet &&
            transferOptions.Mode != TftpTransferMode.Netascii)
        {
            throw new TftpProtocolException(
                "The client write transfer mode is invalid.",
                false,
                0);
        }

        if (request.HasKnownSourceSize && request.KnownSourceSize < 0)
        {
            throw new TftpProtocolException(
                "The known client write source size cannot be negative.",
                false,
                0);
        }

        IPEndPoint endpointCopy = new IPEndPoint(
            serverEndPoint.Address,
            serverEndPoint.Port);
        Request = request;
        TransferOptions = transferOptions;
        Source = source;
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
        EffectiveTimeoutSeconds = transferOptions.InitialTimeoutSeconds;
    }

    internal TftpClientWriteResult StartWrite(CancellationToken cancellationToken)
    {
        ThrowForWriteCancellation(cancellationToken);
        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The client write transfer has already been cancelled.",
                false,
                0);
        }

        if (Started)
        {
            throw new TftpProtocolException(
                "The client write state machine has already started.",
                false,
                0);
        }

        PrepareWriteSource(cancellationToken);
        TftpPacket requestPacket = CreateWriteRequestPacket();
        PendingRequestPacket = requestPacket;
        Started = true;
        RetransmissionTimer.RestartCurrentInterval();
        return CreateWriteResult(
            TftpClientWriteAction.SendRequest,
            requestPacket,
            InitialServerEndPoint,
            0);
    }

    internal TftpClientWriteResult ProcessWritePacket(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        ThrowForWriteCancellation(cancellationToken);
        EnsureActiveWrite();
        TftpPeerValidation peerValidation =
            PeerTracker.ClassifyPeer(peerEndPoint);
        if (peerValidation == TftpPeerValidation.UnexpectedAddress)
        {
            return CreateWriteResult(
                TftpClientWriteAction.IgnoreUnexpectedPeer,
                null,
                null,
                0);
        }

        if (peerValidation == TftpPeerValidation.UnknownTransferId)
        {
            TftpPacket unknownTransferId = CreateWriteErrorPacket(
                TftpErrorCode.UnknownTransferId,
                "Unknown transfer ID.");
            return CreateWriteResult(
                TftpClientWriteAction.RejectUnknownTransferId,
                unknownTransferId,
                peerEndPoint,
                0);
        }

        if (packet == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The client write response packet cannot be null.",
                false,
                0);
        }

        if (EnteredSender)
        {
            return ProcessSenderPacket(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        if (packet.Opcode == TftpOpcode.Error)
        {
            return ProcessInitialWriteError(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        FailWriteIfTotalDurationReached();
        if (packet.Opcode == TftpOpcode.OptionAcknowledgement)
        {
            return ProcessInitialWriteOptionAcknowledgement(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        if (packet.Opcode == TftpOpcode.Acknowledgement)
        {
            return ProcessInitialAcknowledgement(
                packet,
                peerEndPoint,
                cancellationToken);
        }

        return CreateInitialWriteProtocolFailure(
            "The WRQ response must be OACK, ACK block 0, or ERROR.",
            peerEndPoint);
    }

    internal TftpClientWriteResult EvaluateWriteTimer(
        CancellationToken cancellationToken)
    {
        ThrowForWriteCancellation(cancellationToken);
        EnsureActiveWrite();
        if (EnteredSender)
        {
            TftpSenderStateMachine? sender = Sender;
            if (sender == null)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The client write sender state is unavailable.",
                    false,
                    0);
            }

            TftpSenderResult senderResult = sender.EvaluateTimer(
                cancellationToken);
            return MapSenderResult(senderResult);
        }

        TftpRetransmissionResult timerResult =
            RetransmissionTimer.EvaluateTimer();
        if (timerResult.Action == TftpRetransmissionAction.Wait)
        {
            return CreateWriteResult(
                TftpClientWriteAction.Wait,
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
                    "The client write request is unavailable for retransmission.",
                    false,
                    0);
            }

            return CreateWriteResult(
                TftpClientWriteAction.SendRequest,
                requestPacket,
                InitialServerEndPoint,
                0);
        }

        Failed = true;
        throw RetransmissionTimer.CreateTimeoutFailure(timerResult);
    }

    internal void PrepareWriteSource(CancellationToken cancellationToken)
    {
        bool hasKnownSize = Request.HasKnownSourceSize;
        long knownSize = Request.KnownSourceSize;
        if (Source.CanSeek)
        {
            long initialPosition = Source.Position;
            long sourceLength = Source.Length;
            if (initialPosition < 0 || sourceLength < initialPosition)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The seekable client write source position exceeds its length.",
                    false,
                    0);
            }

            long remainingSize = sourceLength - initialPosition;
            if (hasKnownSize && knownSize != remainingSize)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The known client write source size contradicts the seekable stream length.",
                    false,
                    0);
            }

            HasSourceSize = true;
            SourceSize = remainingSize;
            ValidateKnownWriteSize(remainingSize);
            if (TransferOptions.Mode == TftpTransferMode.Octet)
            {
                HasEncodedSourceSize = true;
                EncodedSourceSize = remainingSize;
            }
            else if (TransferOptions.RequestTransferSize)
            {
                EncodedSourceSize = CountRemainingNetasciiSize(
                    initialPosition,
                    cancellationToken);
                HasEncodedSourceSize = true;
            }

            return;
        }

        if (hasKnownSize)
        {
            HasSourceSize = true;
            SourceSize = knownSize;
            ValidateKnownWriteSize(knownSize);
            if (TransferOptions.Mode == TftpTransferMode.Octet)
            {
                HasEncodedSourceSize = true;
                EncodedSourceSize = knownSize;
            }
        }
    }

    internal long CountRemainingNetasciiSize(
        long initialPosition,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        long encodedSize = 0;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Source.Position = initialPosition;
                ThrowForWriteCancellation(cancellationToken);
            }

            int readLength = Source.Read(buffer, 0, buffer.Length);
            if (readLength < 0 || readLength > buffer.Length)
            {
                Source.Position = initialPosition;
                Failed = true;
                throw new TftpProtocolException(
                    "The client write source returned an invalid read length while calculating transfer size.",
                    false,
                    0);
            }

            if (readLength == 0)
            {
                break;
            }

            int index = 0;
            while (index < readLength)
            {
                long increment = 1;
                if (buffer[index] == 10 || buffer[index] == 13)
                {
                    increment = 2;
                }

                if (encodedSize > long.MaxValue - increment)
                {
                    Source.Position = initialPosition;
                    Failed = true;
                    throw new TftpProtocolException(
                        "The encoded client write source size exceeds the supported range.",
                        false,
                        0);
                }

                encodedSize = encodedSize + increment;
                index = index + 1;
            }
        }

        Source.Position = initialPosition;
        ThrowForWriteCancellation(cancellationToken);
        return encodedSize;
    }

    internal void ValidateKnownWriteSize(long sourceSize)
    {
        if (sourceSize > TransferOptions.MaximumTransferSize)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The client write source exceeds the maximum transfer size.",
                false,
                0);
        }
    }

    internal TftpPacket CreateWriteRequestPacket()
    {
        TftpPacket requestPacket = new TftpPacket();
        requestPacket.Opcode = TftpOpcode.WriteRequest;
        requestPacket.Filename = Request.RemoteFilename;
        requestPacket.Mode = PendingWriteModeName();
        TftpPacketCodec codec = new TftpPacketCodec();
        int requestBaseLength = codec.GetEncodedLength(requestPacket);
        TftpOptionNegotiationResult requestOptions =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                TransferOptions,
                TftpTransferDirection.Write,
                HasEncodedSourceSize,
                EncodedSourceSize,
                requestBaseLength);
        if (!requestOptions.Success)
        {
            Failed = true;
            throw new TftpOptionNegotiationException(
                requestOptions.FailureMessage,
                requestOptions.OptionName,
                requestOptions.OptionValue);
        }

        requestPacket.Options = requestOptions.Options;
        codec.GetEncodedLength(requestPacket);
        ActiveRequestedOptions = requestOptions.Options;
        return requestPacket;
    }

    internal TftpClientWriteResult ProcessInitialWriteError(
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
            FailWriteIfTotalDurationReached();
            TftpPacket optionlessRequest = CreateOptionlessWriteRequest();
            ActiveRequestedOptions = [];
            PendingRequestPacket = optionlessRequest;
            RetriedWithoutOptions = true;
            RetransmissionTimer.RecordValidProgress();
            ThrowForWriteCancellation(cancellationToken);
            return CreateWriteResult(
                TftpClientWriteAction.SendRequest,
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

    internal TftpClientWriteResult ProcessInitialWriteOptionAcknowledgement(
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
                    TftpTransferDirection.Write,
                    TransferOptions.InitialTimeoutSeconds,
                    TransferOptions.MaximumTransferSize);
        }

        if (!negotiationResult.Success)
        {
            PeerTracker.LockTransferEndpoint(peerEndPoint);
            Failed = true;
            TftpPacket failurePacket = CreateWriteErrorPacket(
                negotiationResult.ErrorCode,
                negotiationResult.FailureMessage);
            TftpClientWriteResult failureResult = CreateWriteResult(
                TftpClientWriteAction.SendError,
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
        if (HasNegotiatedWriteOption(TftpProtocolValues.TimeoutOption))
        {
            RetransmissionTimer.ApplyNegotiatedTimeout(
                EffectiveTimeoutSeconds);
        }
        else
        {
            RetransmissionTimer.RecordValidProgress();
        }

        return EnterWriteSender(peerEndPoint, cancellationToken);
    }

    internal TftpClientWriteResult ProcessInitialAcknowledgement(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        if (packet.BlockNumber != 0)
        {
            return CreateInitialWriteProtocolFailure(
                "An optionless WRQ response must be ACK block 0.",
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
        RetransmissionTimer.RecordValidProgress();
        return EnterWriteSender(peerEndPoint, cancellationToken);
    }

    internal TftpClientWriteResult EnterWriteSender(
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        TftpSenderStateMachine sender = new TftpSenderStateMachine(
            Source,
            TransferOptions.Mode,
            EffectiveBlockSize,
            RetransmissionTimer);
        Sender = sender;
        EnteredSender = true;
        PendingRequestPacket = null;
        TftpSenderResult senderResult = sender.StartSending(
            cancellationToken);
        ValidatePreparedWriteData(sender);
        return MapSenderResult(senderResult);
    }

    internal TftpClientWriteResult ProcessSenderPacket(
        TftpPacket packet,
        IPEndPoint peerEndPoint,
        CancellationToken cancellationToken)
    {
        TftpSenderStateMachine? sender = Sender;
        if (sender == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The client write sender state is unavailable.",
                false,
                0);
        }

        TftpPacket? lastDataPacket = sender.LastDataPacket;
        if (packet.Opcode == TftpOpcode.OptionAcknowledgement &&
            lastDataPacket != null &&
            lastDataPacket.Opcode == TftpOpcode.Data &&
            lastDataPacket.BlockNumber == 1 &&
            sender.BlockCounter.CurrentBlock == 1 &&
            !sender.BlockCounter.HasPreviousBlock &&
            IsMatchingWriteNegotiation(packet))
        {
            sender.DuplicatePacketCount = sender.DuplicatePacketCount + 1;
            sender.RetransmissionTimer.RestartCurrentInterval();
            TftpSenderResult duplicateResult = sender.CreateSenderResult(
                TftpSenderAction.RetransmitData,
                lastDataPacket,
                0);
            return MapSenderResult(duplicateResult);
        }

        if (packet.Opcode == TftpOpcode.Error)
        {
            Failed = true;
        }

        TftpSenderResult senderResult = sender.ProcessPacket(
            packet,
            cancellationToken);
        if (senderResult.Action == TftpSenderAction.SendData)
        {
            ValidatePreparedWriteData(sender);
        }

        return MapSenderResult(senderResult);
    }

    internal void ValidatePreparedWriteData(TftpSenderStateMachine sender)
    {
        long consumedSourceBytes = sender.BytesTransferred +
            sender.PendingPacketSourceBytes;
        if (consumedSourceBytes > TransferOptions.MaximumTransferSize)
        {
            Failed = true;
            sender.Failed = true;
            throw new TftpProtocolException(
                "The client write source exceeds the maximum transfer size.",
                false,
                0);
        }

        if (HasSourceSize && consumedSourceBytes > SourceSize)
        {
            Failed = true;
            sender.Failed = true;
            throw new TftpProtocolException(
                "The client write source exceeds its declared size.",
                false,
                0);
        }

        if (HasSourceSize &&
            sender.LastDataIsFinal &&
            consumedSourceBytes != SourceSize)
        {
            Failed = true;
            sender.Failed = true;
            throw new TftpProtocolException(
                "The client write source ended before its declared size.",
                false,
                0);
        }
    }

    internal TftpClientWriteResult MapSenderResult(
        TftpSenderResult senderResult)
    {
        TftpClientWriteAction action = TftpClientWriteAction.Wait;
        if (senderResult.Action == TftpSenderAction.SendData)
        {
            action = TftpClientWriteAction.SendData;
        }
        else if (senderResult.Action == TftpSenderAction.RetransmitData)
        {
            action = TftpClientWriteAction.RetransmitData;
        }
        else if (senderResult.Action ==
            TftpSenderAction.IgnoreDuplicateAcknowledgement)
        {
            action = TftpClientWriteAction.IgnoreDuplicateAcknowledgement;
        }
        else if (senderResult.Action == TftpSenderAction.Completed)
        {
            action = TftpClientWriteAction.Completed;
            Completed = true;
        }

        IPEndPoint? targetEndPoint = null;
        if (senderResult.Packet != null)
        {
            targetEndPoint = new IPEndPoint(
                PeerTracker.ExpectedAddress,
                PeerTracker.TransferPort);
        }

        TftpClientWriteResult result = CreateWriteResult(
            action,
            senderResult.Packet,
            targetEndPoint,
            senderResult.WaitMilliseconds);
        result.IsFinalData = senderResult.IsFinalData;
        result.BytesTransferred = senderResult.BytesTransferred;
        result.EncodedBytesTransferred =
            senderResult.EncodedBytesTransferred;
        result.RetransmissionCount = senderResult.RetransmissionCount;
        result.DuplicatePacketCount = senderResult.DuplicatePacketCount;
        return result;
    }

    internal TftpClientWriteResult CreateInitialWriteProtocolFailure(
        string message,
        IPEndPoint peerEndPoint)
    {
        PeerTracker.LockTransferEndpoint(peerEndPoint);
        Failed = true;
        TftpPacket failurePacket = CreateWriteErrorPacket(
            TftpErrorCode.IllegalOperation,
            message);
        TftpClientWriteResult result = CreateWriteResult(
            TftpClientWriteAction.SendError,
            failurePacket,
            peerEndPoint,
            0);
        result.HasFailure = true;
        result.FailureErrorCode = TftpErrorCode.IllegalOperation;
        result.FailureMessage = message;
        return result;
    }

    internal TftpPacket CreateOptionlessWriteRequest()
    {
        TftpPacket requestPacket = new TftpPacket();
        requestPacket.Opcode = TftpOpcode.WriteRequest;
        requestPacket.Filename = Request.RemoteFilename;
        requestPacket.Mode = PendingWriteModeName();
        return requestPacket;
    }

    internal string PendingWriteModeName()
    {
        if (TransferOptions.Mode == TftpTransferMode.Netascii)
        {
            return TftpProtocolValues.NetasciiMode;
        }

        return TftpProtocolValues.OctetMode;
    }

    internal bool HasNegotiatedWriteOption(string optionName)
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

    internal bool IsMatchingWriteNegotiation(TftpPacket packet)
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

    internal TftpPacket CreateWriteErrorPacket(
        TftpErrorCode errorCode,
        string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message;
        return packet;
    }

    internal void FailWriteIfTotalDurationReached()
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

    internal void EnsureActiveWrite()
    {
        if (!Started)
        {
            throw new TftpProtocolException(
                "The client write state machine has not started.",
                false,
                0);
        }

        if (Completed)
        {
            throw new TftpProtocolException(
                "The client write transfer has already completed.",
                false,
                0);
        }

        if (Failed)
        {
            throw new TftpProtocolException(
                "The client write transfer has already failed.",
                false,
                0);
        }

        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The client write transfer has already been cancelled.",
                false,
                0);
        }
    }

    internal void ThrowForWriteCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Cancelled = true;
            throw new OperationCanceledException(cancellationToken);
        }
    }

    internal TftpClientWriteResult CreateWriteResult(
        TftpClientWriteAction action,
        TftpPacket? packet,
        IPEndPoint? targetEndPoint,
        long waitMilliseconds)
    {
        TftpClientWriteResult result = new TftpClientWriteResult();
        result.Action = action;
        result.Packet = packet;
        result.TargetEndPoint = targetEndPoint;
        result.WaitMilliseconds = waitMilliseconds;
        result.EnteredSender = EnteredSender;
        result.RetriedWithoutOptions = RetriedWithoutOptions;
        result.EffectiveBlockSize = EffectiveBlockSize;
        result.EffectiveTimeoutSeconds = EffectiveTimeoutSeconds;
        result.HasSourceSize = HasSourceSize;
        result.SourceSize = SourceSize;
        result.HasEncodedSourceSize = HasEncodedSourceSize;
        result.EncodedSourceSize = EncodedSourceSize;
        result.HasTransferSize = HasTransferSize;
        result.TransferSize = TransferSize;
        result.NegotiatedOptions = NegotiatedOptions;
        result.RetransmissionCount =
            RetransmissionTimer.TotalRetransmissionCount;
        return result;
    }
}
