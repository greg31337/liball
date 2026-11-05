using System;
using System.IO;
using System.Threading;

namespace tftp;

internal class TftpSenderStateMachine
{
    internal Stream Source;
    internal NetasciiEncoder Encoder;
    internal int BlockSize;
    internal TftpRetransmissionTimer RetransmissionTimer;
    internal TftpBlockCounter BlockCounter = new TftpBlockCounter();
    internal byte[] ReadBuffer;
    internal int ReadBufferOffset = 0;
    internal int ReadBufferLength = 0;
    internal bool SourceEnded = false;
    internal bool Started = false;
    internal bool Completed = false;
    internal bool Failed = false;
    internal bool Cancelled = false;
    internal bool LastDataIsFinal = false;
    internal TftpPacket? LastDataPacket = null;
    internal long PendingPacketSourceBytes = 0;
    internal long PendingPacketEncodedBytes = 0;
    internal long BytesTransferred = 0;
    internal long EncodedBytesTransferred = 0;
    internal int DuplicatePacketCount = 0;

    internal TftpSenderStateMachine(
        Stream source,
        TftpTransferMode mode,
        int blockSize,
        TftpRetransmissionTimer retransmissionTimer)
    {
        if (source == null)
        {
            throw new TftpProtocolException(
                "The sender source stream cannot be null.",
                false,
                0);
        }

        if (!source.CanRead)
        {
            throw new TftpProtocolException(
                "The sender source stream must be readable.",
                false,
                0);
        }

        if (mode != TftpTransferMode.Octet && mode != TftpTransferMode.Netascii)
        {
            throw new TftpProtocolException(
                "The sender transfer mode is invalid.",
                false,
                0);
        }

        if (blockSize < 1 || blockSize > TftpProtocolValues.MaximumBlockSize)
        {
            throw new TftpProtocolException(
                "The sender block size is outside the supported range.",
                false,
                0);
        }

        if (retransmissionTimer == null)
        {
            throw new TftpProtocolException(
                "The sender retransmission timer cannot be null.",
                false,
                0);
        }

        Source = source;
        Encoder = new NetasciiEncoder(mode);
        BlockSize = blockSize;
        RetransmissionTimer = retransmissionTimer;
        ReadBuffer = new byte[blockSize];
    }

    internal TftpSenderResult StartSending(CancellationToken cancellationToken)
    {
        ThrowForCancellation(cancellationToken);
        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The sender transfer has already been cancelled.",
                false,
                0);
        }

        if (Started)
        {
            throw new TftpProtocolException(
                "The sender state machine has already started.",
                false,
                0);
        }

        Started = true;
        TftpPacket packet = FillNextDataPacket(cancellationToken);
        RetransmissionTimer.RestartCurrentInterval();
        return CreateSenderResult(
            TftpSenderAction.SendData,
            packet,
            0);
    }

    internal TftpSenderResult ProcessPacket(
        TftpPacket packet,
        CancellationToken cancellationToken)
    {
        ThrowForCancellation(cancellationToken);
        EnsureActiveSender();
        if (packet == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The received sender packet cannot be null.",
                false,
                0);
        }

        if (packet.Opcode == TftpOpcode.Error)
        {
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

        long packetTime =
            RetransmissionTimer.TimeSource.GetTimestampMilliseconds();
        long packetElapsed = RetransmissionTimer.GetNonnegativeElapsed(
            RetransmissionTimer.TransferStartedMilliseconds,
            packetTime);
        if (packetElapsed >= RetransmissionTimer.MaximumDurationMilliseconds)
        {
            FailSenderForTotalDuration(packetElapsed);
        }

        if (packet.Opcode != TftpOpcode.Acknowledgement)
        {
            FailWithProtocolError(
                "The sender received a packet other than ACK or ERROR.");
        }

        TftpBlockClassification classification =
            BlockCounter.ClassifyAcknowledgementBlock(packet.BlockNumber);
        if (classification == TftpBlockClassification.Previous ||
            classification == TftpBlockClassification.HandshakeAcknowledgement)
        {
            DuplicatePacketCount = DuplicatePacketCount + 1;
            return CreateSenderResult(
                TftpSenderAction.IgnoreDuplicateAcknowledgement,
                null,
                0);
        }

        if (classification != TftpBlockClassification.Expected)
        {
            FailWithProtocolError(
                "The sender received an unexpected ACK block number.");
        }

        BytesTransferred = BytesTransferred + PendingPacketSourceBytes;
        EncodedBytesTransferred =
            EncodedBytesTransferred + PendingPacketEncodedBytes;
        PendingPacketSourceBytes = 0;
        PendingPacketEncodedBytes = 0;
        RetransmissionTimer.RecordValidProgress();

        if (LastDataIsFinal)
        {
            Completed = true;
            LastDataPacket = null;
            return CreateSenderResult(
                TftpSenderAction.Completed,
                null,
                0);
        }

        BlockCounter.AdvanceBlock();
        TftpPacket nextPacket = FillNextDataPacket(cancellationToken);
        RetransmissionTimer.RestartCurrentInterval();
        return CreateSenderResult(
            TftpSenderAction.SendData,
            nextPacket,
            0);
    }

    internal TftpSenderResult EvaluateTimer(CancellationToken cancellationToken)
    {
        ThrowForCancellation(cancellationToken);
        EnsureActiveSender();
        TftpRetransmissionResult timerResult =
            RetransmissionTimer.EvaluateTimer();
        if (timerResult.Action == TftpRetransmissionAction.Wait)
        {
            return CreateSenderResult(
                TftpSenderAction.Wait,
                null,
                timerResult.WaitMilliseconds);
        }

        if (timerResult.Action == TftpRetransmissionAction.Retransmit)
        {
            TftpPacket? retainedPacket = LastDataPacket;
            if (retainedPacket == null)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The sender has no DATA packet available for retransmission.",
                    false,
                    0);
            }

            return CreateSenderResult(
                TftpSenderAction.RetransmitData,
                retainedPacket,
                0);
        }

        Failed = true;
        throw RetransmissionTimer.CreateTimeoutFailure(timerResult);
    }

    internal TftpPacket FillNextDataPacket(CancellationToken cancellationToken)
    {
        byte[] payloadBuffer = new byte[BlockSize];
        int payloadLength = 0;
        long sourceBytes = 0;
        while (payloadLength < BlockSize)
        {
            if (ReadBufferOffset < ReadBufferLength)
            {
                NetasciiEncodeResult encodeResult = Encoder.EncodeBytes(
                    ReadBuffer.AsSpan(
                        ReadBufferOffset,
                        ReadBufferLength - ReadBufferOffset),
                    payloadBuffer.AsSpan(
                        payloadLength,
                        BlockSize - payloadLength));
                ReadBufferOffset = ReadBufferOffset + encodeResult.BytesConsumed;
                payloadLength = payloadLength + encodeResult.BytesWritten;
                sourceBytes = sourceBytes + encodeResult.BytesConsumed;
                if (encodeResult.BytesConsumed == 0 &&
                    encodeResult.BytesWritten == 0)
                {
                    FailWithProtocolError(
                        "The sender content encoder made no progress.");
                }

                continue;
            }

            if (Encoder.HasPendingByte)
            {
                NetasciiEncodeResult pendingResult = Encoder.EncodeBytes(
                    ReadOnlySpan<byte>.Empty,
                    payloadBuffer.AsSpan(
                        payloadLength,
                        BlockSize - payloadLength));
                payloadLength = payloadLength + pendingResult.BytesWritten;
                if (pendingResult.BytesWritten == 0)
                {
                    FailWithProtocolError(
                        "The sender content encoder could not flush pending output.");
                }

                continue;
            }

            if (SourceEnded)
            {
                NetasciiEncodeResult flushResult = Encoder.EncodeBytes(
                    ReadOnlySpan<byte>.Empty,
                    payloadBuffer.AsSpan(
                        payloadLength,
                        BlockSize - payloadLength));
                payloadLength = payloadLength + flushResult.BytesWritten;
                if (flushResult.BytesWritten == 0)
                {
                    break;
                }

                continue;
            }

            ThrowForCancellation(cancellationToken);
            int readLength = Source.Read(ReadBuffer, 0, ReadBuffer.Length);
            ThrowForCancellation(cancellationToken);
            if (readLength < 0 || readLength > ReadBuffer.Length)
            {
                FailWithProtocolError(
                    "The sender source returned an invalid read length.");
            }

            ReadBufferOffset = 0;
            ReadBufferLength = readLength;
            if (readLength == 0)
            {
                SourceEnded = true;
            }
        }

        byte[] payload = new byte[payloadLength];
        payloadBuffer.AsSpan(0, payloadLength).CopyTo(payload);
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = BlockCounter.CurrentBlock;
        packet.Payload = payload;
        LastDataIsFinal = payloadLength < BlockSize;
        LastDataPacket = packet;
        PendingPacketSourceBytes = sourceBytes;
        PendingPacketEncodedBytes = payloadLength;
        return packet;
    }

    internal void EnsureActiveSender()
    {
        if (!Started)
        {
            throw new TftpProtocolException(
                "The sender state machine has not started.",
                false,
                0);
        }

        if (Completed)
        {
            throw new TftpProtocolException(
                "The sender transfer has already completed.",
                false,
                0);
        }

        if (Failed)
        {
            throw new TftpProtocolException(
                "The sender transfer has already failed.",
                false,
                0);
        }

        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The sender transfer has already been cancelled.",
                false,
                0);
        }
    }

    internal void ThrowForCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Cancelled = true;
            throw new OperationCanceledException(cancellationToken);
        }
    }

    internal void FailWithProtocolError(string message)
    {
        Failed = true;
        throw new TftpProtocolException(message, false, 0);
    }

    internal void FailSenderForTotalDuration(long elapsedMilliseconds)
    {
        Failed = true;
        TftpRetransmissionResult totalFailure =
            RetransmissionTimer.CreateTimerResult(
                TftpRetransmissionAction.TotalDurationReached,
                0,
                elapsedMilliseconds);
        throw RetransmissionTimer.CreateTimeoutFailure(totalFailure);
    }

    internal TftpSenderResult CreateSenderResult(
        TftpSenderAction action,
        TftpPacket? packet,
        long waitMilliseconds)
    {
        TftpSenderResult result = new TftpSenderResult();
        result.Action = action;
        result.Packet = packet;
        result.WaitMilliseconds = waitMilliseconds;
        result.IsFinalData = LastDataIsFinal;
        result.BytesTransferred = BytesTransferred;
        result.EncodedBytesTransferred = EncodedBytesTransferred;
        result.RetransmissionCount =
            RetransmissionTimer.TotalRetransmissionCount;
        result.DuplicatePacketCount = DuplicatePacketCount;
        return result;
    }
}
