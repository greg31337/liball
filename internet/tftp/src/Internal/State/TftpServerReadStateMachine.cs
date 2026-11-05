using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace tftp;

internal class TftpServerReadStateMachine : IDisposable
{
    internal TftpPacket RequestPacket;
    internal TftpServerRequest ServerRequest;
    internal TftpServerStorageFlow StorageFlow;
    internal TftpServerOptions ServerOptions;
    internal ITftpTimeSource TimeSource;
    internal Stream? ReadContent;
    internal bool OwnsReadContent;
    internal TftpRetransmissionTimer? RetransmissionTimer;
    internal TftpSenderStateMachine? Sender;
    internal TftpPacket? RetainedOptionAcknowledgement;
    internal List<TftpOption> NegotiatedOptions;
    internal int EffectiveBlockSize;
    internal int EffectiveTimeoutSeconds;
    internal bool HasTransferSize;
    internal long TransferSize;
    internal bool Started;
    internal bool AwaitingOptionAcknowledgement;
    internal bool Completed;
    internal bool Failed;
    internal bool Cancelled;

    internal TftpServerReadStateMachine(
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
                "The server read state inputs cannot be null.",
                false,
                0);
        }

        ValidateServerOptions(serverOptions);
        RequestPacket = TftpServerSession.CopyPacket(requestPacket);
        ServerRequest = TftpServerSession.CreateServerRequest(
            RequestPacket,
            clientEndPoint);
        StorageFlow = new TftpServerStorageFlow(storage);
        ServerOptions = CopyServerOptions(serverOptions);
        TimeSource = timeSource;
        ReadContent = null;
        OwnsReadContent = false;
        RetransmissionTimer = null;
        Sender = null;
        RetainedOptionAcknowledgement = null;
        NegotiatedOptions = [];
        EffectiveBlockSize = TftpProtocolValues.DefaultBlockSize;
        EffectiveTimeoutSeconds = serverOptions.InitialTimeoutSeconds;
        HasTransferSize = false;
        TransferSize = 0;
        Started = false;
        AwaitingOptionAcknowledgement = false;
        Completed = false;
        Failed = false;
        Cancelled = false;
    }

    internal TftpServerReadResult StartRead(
        CancellationToken cancellationToken)
    {
        ThrowForReadCancellation(cancellationToken);
        if (Started)
        {
            throw new TftpProtocolException(
                "The server read state machine has already started.",
                false,
                0);
        }

        Started = true;
        string requestFailure = ValidateReadRequest();
        if (requestFailure.Length != 0)
        {
            return CreateReadFailure(
                TftpErrorCode.IllegalOperation,
                requestFailure,
                false);
        }

        int requestBaseLength = TftpProtocolValues.OpcodeLength +
            RequestPacket.Filename.Length + 1 + RequestPacket.Mode.Length + 1;
        if (requestBaseLength > ServerOptions.MaximumRequestPacketLength ||
            requestBaseLength > TftpProtocolValues.MaximumRequestPacketLength)
        {
            return CreateReadFailure(
                TftpErrorCode.IllegalOperation,
                "The RRQ packet exceeds the configured request length limit.",
                false);
        }

        TftpOptionNegotiationResult preliminaryNegotiation =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                RequestPacket.Options,
                ServerOptions,
                requestBaseLength,
                TftpTransferDirection.Read,
                false,
                0);
        if (!preliminaryNegotiation.Success)
        {
            return CreateOptionReadFailure(preliminaryNegotiation);
        }

        if (preliminaryNegotiation.EncodedLength >
            ServerOptions.MaximumRequestPacketLength)
        {
            return CreateReadFailure(
                TftpErrorCode.IllegalOperation,
                "The RRQ packet exceeds the configured request length limit.",
                false);
        }

        ServerRequest.RequestedOptions =
            TftpServerSession.CopyOptions(RequestPacket.Options);
        TftpStorageOpenResult openResult = StorageFlow.OpenRead(
            ServerRequest,
            cancellationToken);
        if (!openResult.Success)
        {
            return CreateReadFailure(
                openResult.ErrorCode,
                openResult.ErrorMessage,
                false);
        }

        Stream? openedContent = openResult.Content;
        if (openedContent == null)
        {
            throw new TftpStorageException(
                "The validated read result has no content stream.",
                "OpenRead",
                null);
        }

        ReadContent = openedContent;
        OwnsReadContent = openResult.OwnsContent;
        ThrowForReadCancellation(cancellationToken);
        if (openResult.HasKnownSize &&
            openResult.KnownSize > ServerOptions.MaximumTransferSize)
        {
            return CreateReadFailure(
                TftpErrorCode.StorageExhausted,
                "The requested read exceeds the server transfer capacity.",
                false);
        }

        TftpOptionNegotiationResult negotiation =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                RequestPacket.Options,
                ServerOptions,
                requestBaseLength,
                TftpTransferDirection.Read,
                openResult.HasKnownSize,
                openResult.KnownSize);
        if (!negotiation.Success)
        {
            return CreateOptionReadFailure(negotiation);
        }

        NegotiatedOptions =
            TftpServerSession.CopyOptions(negotiation.Options);
        ServerRequest.EffectiveOptions =
            TftpServerSession.CopyOptions(negotiation.Options);
        EffectiveBlockSize = negotiation.EffectiveBlockSize;
        EffectiveTimeoutSeconds = negotiation.EffectiveTimeoutSeconds;
        HasTransferSize = negotiation.HasTransferSize;
        TransferSize = negotiation.TransferSize;
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
        TftpSenderStateMachine sender = new TftpSenderStateMachine(
            openedContent,
            ServerRequest.Mode,
            EffectiveBlockSize,
            timer);
        Sender = sender;
        if (NegotiatedOptions.Count > 0)
        {
            TftpPacket optionAcknowledgement = new TftpPacket();
            optionAcknowledgement.Opcode =
                TftpOpcode.OptionAcknowledgement;
            optionAcknowledgement.Options =
                TftpServerSession.CopyOptions(NegotiatedOptions);
            RetainedOptionAcknowledgement = optionAcknowledgement;
            AwaitingOptionAcknowledgement = true;
            timer.RestartCurrentInterval();
            return CreateReadResult(
                TftpServerReadAction.SendOptionAcknowledgement,
                optionAcknowledgement,
                0);
        }

        TftpSenderResult senderResult = sender.StartSending(
            cancellationToken);
        return MapSenderResult(senderResult);
    }

    internal TftpServerReadResult ProcessReadPacket(
        TftpPacket packet,
        CancellationToken cancellationToken)
    {
        ThrowForReadCancellation(cancellationToken);
        EnsureActiveRead();
        if (packet == null)
        {
            return CreateReadFailure(
                TftpErrorCode.IllegalOperation,
                "The server read received a null packet.",
                false);
        }

        FailIfTotalDurationReached();
        if (packet.Opcode == TftpOpcode.Error)
        {
            Failed = true;
            CloseReadContent();
            string remoteMessage = packet.ErrorMessage;
            if (remoteMessage == null)
            {
                remoteMessage = "";
            }

            throw new TftpRemoteErrorException(
                packet.ErrorCode,
                remoteMessage);
        }

        if (AwaitingOptionAcknowledgement)
        {
            if (packet.Opcode != TftpOpcode.Acknowledgement ||
                packet.BlockNumber != 0)
            {
                return CreateReadFailure(
                    TftpErrorCode.IllegalOperation,
                    "An RRQ OACK must be confirmed by ACK block 0.",
                    false);
            }

            AwaitingOptionAcknowledgement = false;
            RetainedOptionAcknowledgement = null;
            AvailableTimer().RecordValidProgress();
            TftpSenderStateMachine optionSender = AvailableSender();
            TftpSenderResult initialData = optionSender.StartSending(
                cancellationToken);
            return MapSenderResult(initialData);
        }

        if (packet.Opcode != TftpOpcode.Acknowledgement)
        {
            return CreateReadFailure(
                TftpErrorCode.IllegalOperation,
                "The server read expected an ACK packet.",
                false);
        }

        TftpSenderStateMachine sender = AvailableSender();
        TftpBlockClassification classification =
            sender.BlockCounter.ClassifyAcknowledgementBlock(
                packet.BlockNumber);
        if (classification != TftpBlockClassification.Expected &&
            classification != TftpBlockClassification.Previous &&
            classification !=
                TftpBlockClassification.HandshakeAcknowledgement)
        {
            return CreateReadFailure(
                TftpErrorCode.IllegalOperation,
                "The server read received an unexpected ACK block number.",
                false);
        }

        TftpSenderResult senderResult = sender.ProcessPacket(
            packet,
            cancellationToken);
        return MapSenderResult(senderResult);
    }

    internal TftpServerReadResult EvaluateReadTimer(
        CancellationToken cancellationToken)
    {
        ThrowForReadCancellation(cancellationToken);
        EnsureActiveRead();
        TftpRetransmissionTimer timer = AvailableTimer();
        TftpRetransmissionResult timerResult = timer.EvaluateTimer();
        if (timerResult.Action == TftpRetransmissionAction.Wait)
        {
            return CreateReadResult(
                TftpServerReadAction.Wait,
                null,
                timerResult.WaitMilliseconds);
        }

        if (timerResult.Action == TftpRetransmissionAction.Retransmit)
        {
            TftpPacket? retainedPacket = RetainedOptionAcknowledgement;
            if (!AwaitingOptionAcknowledgement)
            {
                retainedPacket = AvailableSender().LastDataPacket;
            }

            if (retainedPacket == null)
            {
                Failed = true;
                CloseReadContent();
                throw new TftpProtocolException(
                    "The server read has no packet available for retransmission.",
                    false,
                    0);
            }

            return CreateReadResult(
                TftpServerReadAction.RetransmitPacket,
                retainedPacket,
                0);
        }

        Failed = true;
        CloseReadContent();
        throw timer.CreateTimeoutFailure(timerResult);
    }

    internal TftpServerReadResult MapSenderResult(
        TftpSenderResult senderResult)
    {
        TftpServerReadAction action = TftpServerReadAction.Wait;
        if (senderResult.Action == TftpSenderAction.SendData)
        {
            action = TftpServerReadAction.SendData;
            TftpSenderStateMachine sender = AvailableSender();
            if (sender.PendingPacketEncodedBytes >
                ServerOptions.MaximumTransferSize -
                    sender.EncodedBytesTransferred)
            {
                return CreateReadFailure(
                    TftpErrorCode.StorageExhausted,
                    "The requested read exceeds the server transfer capacity.",
                    false);
            }
        }
        else if (senderResult.Action ==
            TftpSenderAction.IgnoreDuplicateAcknowledgement)
        {
            action = TftpServerReadAction.IgnoreDuplicateAcknowledgement;
        }
        else if (senderResult.Action == TftpSenderAction.Completed)
        {
            action = TftpServerReadAction.Completed;
            Completed = true;
            CloseReadContent();
        }

        TftpServerReadResult result = CreateReadResult(
            action,
            senderResult.Packet,
            senderResult.WaitMilliseconds);
        result.IsFinalData = senderResult.IsFinalData;
        result.BytesTransferred = senderResult.BytesTransferred;
        result.EncodedBytesTransferred =
            senderResult.EncodedBytesTransferred;
        result.RetransmissionCount = senderResult.RetransmissionCount;
        result.DuplicatePacketCount = senderResult.DuplicatePacketCount;
        return result;
    }

    internal TftpServerReadResult CreateReadResult(
        TftpServerReadAction action,
        TftpPacket? packet,
        long waitMilliseconds)
    {
        TftpServerReadResult result = new TftpServerReadResult();
        result.Action = action;
        result.Packet = packet;
        result.WaitMilliseconds = waitMilliseconds;
        result.EffectiveBlockSize = EffectiveBlockSize;
        result.EffectiveTimeoutSeconds = EffectiveTimeoutSeconds;
        result.HasTransferSize = HasTransferSize;
        result.TransferSize = TransferSize;
        result.NegotiatedOptions =
            TftpServerSession.CopyOptions(NegotiatedOptions);
        TftpSenderStateMachine? sender = Sender;
        if (sender != null)
        {
            result.BytesTransferred = sender.BytesTransferred;
            result.EncodedBytesTransferred = sender.EncodedBytesTransferred;
            result.RetransmissionCount =
                AvailableTimer().TotalRetransmissionCount;
            result.DuplicatePacketCount = sender.DuplicatePacketCount;
            result.IsFinalData = sender.LastDataIsFinal;
        }

        return result;
    }

    internal TftpServerReadResult CreateReadFailure(
        TftpErrorCode errorCode,
        string message,
        bool optionFailure)
    {
        Failed = true;
        AwaitingOptionAcknowledgement = false;
        RetainedOptionAcknowledgement = null;
        CloseReadContent();
        TftpPacket errorPacket = TftpServerErrorMapper.CreateErrorPacket(
            errorCode,
            message,
            ServerOptions.MaximumRequestPacketLength);

        TftpServerReadResult result = CreateReadResult(
            TftpServerReadAction.SendError,
            errorPacket,
            0);
        result.HasFailure = true;
        result.IsOptionFailure = optionFailure;
        result.FailureErrorCode = errorPacket.ErrorCode;
        result.FailureMessage = errorPacket.ErrorMessage;
        result.IsFinalData = false;
        return result;
    }

    internal TftpServerReadResult CreateOptionReadFailure(
        TftpOptionNegotiationResult negotiation)
    {
        TftpServerReadResult result = CreateReadFailure(
            negotiation.ErrorCode,
            negotiation.FailureMessage,
            true);
        result.FailureOptionName = negotiation.OptionName;
        result.FailureOptionValue = negotiation.OptionValue;
        return result;
    }

    internal string ValidateReadRequest()
    {
        if (RequestPacket.Opcode != TftpOpcode.ReadRequest)
        {
            return "The server read requires an RRQ packet.";
        }

        if (RequestPacket.Filename == null ||
            RequestPacket.Filename.Length == 0)
        {
            return "The RRQ filename cannot be empty.";
        }

        if (RequestPacket.Filename.Length >
            ServerOptions.MaximumFilenameLength)
        {
            return "The RRQ filename exceeds the configured length limit.";
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
            return "The RRQ mode cannot be empty.";
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
            return "The RRQ transfer mode is not supported.";
        }

        if (RequestPacket.Options == null)
        {
            return "The RRQ option list cannot be null.";
        }

        ServerRequest.Filename = RequestPacket.Filename;
        ServerRequest.Direction = TftpTransferDirection.Read;
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
                return "The RRQ " + fieldName +
                    " cannot contain an embedded NUL.";
            }

            if (character > 127)
            {
                return "The RRQ " + fieldName +
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
            CloseReadContent();
            TftpRetransmissionResult totalFailure = timer.CreateTimerResult(
                TftpRetransmissionAction.TotalDurationReached,
                0,
                elapsed);
            throw timer.CreateTimeoutFailure(totalFailure);
        }
    }

    internal void EnsureActiveRead()
    {
        if (!Started)
        {
            throw new TftpProtocolException(
                "The server read state machine has not started.",
                false,
                0);
        }

        if (Completed)
        {
            throw new TftpProtocolException(
                "The server read transfer has already completed.",
                false,
                0);
        }

        if (Failed)
        {
            throw new TftpProtocolException(
                "The server read transfer has already failed.",
                false,
                0);
        }

        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The server read transfer has already been cancelled.",
                false,
                0);
        }
    }

    internal void ThrowForReadCancellation(
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Cancelled = true;
            CloseReadContent();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    internal void CloseReadContent()
    {
        Stream? content = ReadContent;
        ReadContent = null;
        if (content != null && OwnsReadContent)
        {
            content.Dispose();
        }

        OwnsReadContent = false;
    }

    internal TftpSenderStateMachine AvailableSender()
    {
        TftpSenderStateMachine? sender = Sender;
        if (sender == null)
        {
            throw new TftpProtocolException(
                "The server read sender state is unavailable.",
                false,
                0);
        }

        return sender;
    }

    internal TftpRetransmissionTimer AvailableTimer()
    {
        TftpRetransmissionTimer? timer = RetransmissionTimer;
        if (timer == null)
        {
            throw new TftpProtocolException(
                "The server read timer state is unavailable.",
                false,
                0);
        }

        return timer;
    }

    internal static TftpServerOptions CopyServerOptions(
        TftpServerOptions source)
    {
        TftpServerOptions copy = new TftpServerOptions();
        copy.EnableBlockSize = source.EnableBlockSize;
        copy.MaximumBlockSize = source.MaximumBlockSize;
        copy.EnableTimeout = source.EnableTimeout;
        copy.EnableTransferSize = source.EnableTransferSize;
        copy.MaximumSessions = source.MaximumSessions;
        copy.MaximumFilenameLength = source.MaximumFilenameLength;
        copy.MaximumRequestPacketLength = source.MaximumRequestPacketLength;
        copy.MaximumTransferSize = source.MaximumTransferSize;
        copy.InitialTimeoutSeconds = source.InitialTimeoutSeconds;
        copy.MaximumTimeoutSeconds = source.MaximumTimeoutSeconds;
        copy.MaximumRetries = source.MaximumRetries;
        copy.MaximumSessionDurationSeconds =
            source.MaximumSessionDurationSeconds;
        copy.DallyDurationSeconds = source.DallyDurationSeconds;
        copy.StopMode = source.StopMode;
        return copy;
    }

    internal static void ValidateServerOptions(TftpServerOptions options)
    {
        if (options.MaximumFilenameLength < 1)
        {
            throw new TftpProtocolException(
                "The server filename length limit must be positive.",
                false,
                0);
        }

        if (options.MaximumRequestPacketLength < 5 ||
            options.MaximumRequestPacketLength >
                TftpProtocolValues.MaximumRequestPacketLength)
        {
            throw new TftpProtocolException(
                "The server request packet length limit is invalid.",
                false,
                0);
        }

        if (options.MaximumTransferSize < 0)
        {
            throw new TftpProtocolException(
                "The server transfer size limit cannot be negative.",
                false,
                0);
        }

        if (options.MaximumBlockSize < TftpProtocolValues.MinimumBlockSize ||
            options.MaximumBlockSize > TftpProtocolValues.MaximumBlockSize)
        {
            throw new TftpProtocolException(
                "The server block size limit is invalid.",
                false,
                0);
        }

        if (options.InitialTimeoutSeconds < 1 ||
            options.MaximumTimeoutSeconds < options.InitialTimeoutSeconds)
        {
            throw new TftpProtocolException(
                "The server retransmission timeout settings are invalid.",
                false,
                0);
        }

        if (options.MaximumRetries < 0 ||
            options.MaximumSessionDurationSeconds < 1)
        {
            throw new TftpProtocolException(
                "The server retry or duration limit is invalid.",
                false,
                0);
        }
    }

    /// <summary>
    /// Releases owned read content that remains open.
    /// </summary>
    public void Dispose()
    {
        CloseReadContent();
    }
}
