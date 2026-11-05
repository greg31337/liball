using System;
using System.IO;
using System.Threading;

namespace tftp;

internal class TftpReceiverStateMachine
{
    internal Stream Destination;
    internal NetasciiDecoder Decoder;
    internal int BlockSize;
    internal long MaximumTransferSize;
    internal TftpRetransmissionTimer RetransmissionTimer;
    internal long DallyDurationMilliseconds;
    internal TftpBlockCounter BlockCounter = new TftpBlockCounter();
    internal bool Started = false;
    internal bool Dallying = false;
    internal bool Completed = false;
    internal bool Failed = false;
    internal bool Cancelled = false;
    internal long DallyStartedMilliseconds = 0;
    internal ushort FinalBlockNumber = 0;
    internal int FinalDataLength = 0;
    internal TftpPacket? RetainedPacket = null;
    internal long BytesTransferred = 0;
    internal long EncodedBytesTransferred = 0;
    internal int DuplicatePacketCount = 0;

    internal TftpReceiverStateMachine(
        Stream destination,
        TftpTransferMode mode,
        int blockSize,
        long maximumTransferSize,
        TftpRetransmissionTimer retransmissionTimer,
        int dallyDurationSeconds)
    {
        if (destination == null)
        {
            throw new TftpProtocolException(
                "The receiver destination stream cannot be null.",
                false,
                0);
        }

        if (!destination.CanWrite)
        {
            throw new TftpProtocolException(
                "The receiver destination stream must be writable.",
                false,
                0);
        }

        if (mode != TftpTransferMode.Octet && mode != TftpTransferMode.Netascii)
        {
            throw new TftpProtocolException(
                "The receiver transfer mode is invalid.",
                false,
                0);
        }

        if (blockSize < 1 || blockSize > TftpProtocolValues.MaximumBlockSize)
        {
            throw new TftpProtocolException(
                "The receiver block size is outside the supported range.",
                false,
                0);
        }

        if (maximumTransferSize < 0)
        {
            throw new TftpProtocolException(
                "The receiver maximum transfer size cannot be negative.",
                false,
                0);
        }

        if (retransmissionTimer == null)
        {
            throw new TftpProtocolException(
                "The receiver retransmission timer cannot be null.",
                false,
                0);
        }

        if (dallyDurationSeconds < 0)
        {
            throw new TftpProtocolException(
                "The receiver dally duration cannot be negative.",
                false,
                0);
        }

        Destination = destination;
        Decoder = new NetasciiDecoder(mode);
        BlockSize = blockSize;
        MaximumTransferSize = maximumTransferSize;
        RetransmissionTimer = retransmissionTimer;
        DallyDurationMilliseconds = dallyDurationSeconds * 1000L;
    }

    internal TftpReceiverResult StartReceiving(
        TftpPacket retainedPacket,
        CancellationToken cancellationToken)
    {
        ThrowForReceiverCancellation(cancellationToken);
        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The receiver transfer has already been cancelled.",
                false,
                0);
        }

        if (Started)
        {
            throw new TftpProtocolException(
                "The receiver state machine has already started.",
                false,
                0);
        }

        if (retainedPacket == null)
        {
            throw new TftpProtocolException(
                "The receiver requires an initial retained packet.",
                false,
                0);
        }

        if (retainedPacket.Opcode != TftpOpcode.ReadRequest &&
            retainedPacket.Opcode != TftpOpcode.WriteRequest &&
            retainedPacket.Opcode != TftpOpcode.Acknowledgement &&
            retainedPacket.Opcode != TftpOpcode.OptionAcknowledgement)
        {
            throw new TftpProtocolException(
                "The initial retained receiver packet must be a request, ACK, or OACK.",
                false,
                0);
        }

        RetainedPacket = retainedPacket;
        Started = true;
        RetransmissionTimer.RestartCurrentInterval();
        return EvaluateReceiverTimer(cancellationToken);
    }

    internal TftpReceiverResult ProcessDataPacket(
        TftpPacket packet,
        CancellationToken cancellationToken)
    {
        ThrowForReceiverCancellation(cancellationToken);
        EnsureActiveReceiver();
        if (packet == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The received receiver packet cannot be null.",
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
            FailReceiverForTotalDuration(packetElapsed);
        }

        if (packet.Opcode != TftpOpcode.Data)
        {
            FailReceiverWithProtocolError(
                "The receiver received a packet other than DATA or ERROR.",
                false,
                0);
        }

        byte[]? payload = packet.Payload;
        if (payload == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The received DATA payload cannot be null.",
                false,
                0);
        }

        if (payload.Length > BlockSize)
        {
            FailReceiverWithProtocolError(
                "The received DATA payload exceeds the active block size.",
                false,
                0);
        }

        if (Dallying)
        {
            return ProcessDallyPacket(packet, payload, cancellationToken);
        }

        TftpBlockClassification classification =
            BlockCounter.ClassifyDataBlock(packet.BlockNumber);
        if (classification == TftpBlockClassification.Previous)
        {
            TftpPacket? duplicateAcknowledgement = RetainedPacket;
            if (duplicateAcknowledgement == null)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The receiver has no ACK for duplicate DATA.",
                    false,
                    0);
            }

            DuplicatePacketCount = DuplicatePacketCount + 1;
            return CreateReceiverResult(
                TftpReceiverAction.ReacknowledgeDuplicateData,
                duplicateAcknowledgement,
                0,
                false);
        }

        if (classification != TftpBlockClassification.Expected)
        {
            FailReceiverWithProtocolError(
                "The receiver received an unexpected DATA block number.",
                false,
                0);
        }

        bool isFinalData = payload.Length < BlockSize;
        int decodedLength = DecodeAndWriteDataPayload(
            payload,
            isFinalData,
            cancellationToken);
        BytesTransferred = BytesTransferred + decodedLength;
        EncodedBytesTransferred =
            EncodedBytesTransferred + payload.Length;

        TftpPacket acknowledgement = new TftpPacket();
        acknowledgement.Opcode = TftpOpcode.Acknowledgement;
        acknowledgement.BlockNumber = packet.BlockNumber;
        RetainedPacket = acknowledgement;
        RetransmissionTimer.RecordValidProgress();

        if (isFinalData)
        {
            Dallying = true;
            FinalBlockNumber = packet.BlockNumber;
            FinalDataLength = payload.Length;
            DallyStartedMilliseconds =
                RetransmissionTimer.IntervalStartedMilliseconds;
        }
        else
        {
            BlockCounter.AdvanceBlock();
        }

        return CreateReceiverResult(
            TftpReceiverAction.SendAcknowledgement,
            acknowledgement,
            0,
            isFinalData);
    }

    internal int DecodeAndWriteDataPayload(
        byte[] payload,
        bool isFinalData,
        CancellationToken cancellationToken)
    {
        byte[] decodedPayload = new byte[payload.Length];
        NetasciiDecodeResult decodeResult = Decoder.DecodeBytes(
            payload,
            decodedPayload);
        if (!decodeResult.Success)
        {
            int packetOffset = 0;
            bool hasPacketOffset = false;
            if (decodeResult.HasFailureOffset)
            {
                hasPacketOffset = true;
                packetOffset = TftpProtocolValues.DataHeaderLength +
                    decodeResult.FailureOffset;
            }

            FailReceiverWithProtocolError(
                decodeResult.FailureMessage,
                hasPacketOffset,
                packetOffset);
        }

        if (decodeResult.BytesConsumed != payload.Length ||
            decodeResult.NeedsMoreOutput)
        {
            FailReceiverWithProtocolError(
                "The receiver content decoder did not consume the DATA payload.",
                false,
                0);
        }

        if (decodeResult.BytesWritten >
            MaximumTransferSize - BytesTransferred)
        {
            FailReceiverWithProtocolError(
                "The received content exceeds the maximum transfer size.",
                false,
                0);
        }

        if (isFinalData)
        {
            NetasciiDecodeResult finishResult = Decoder.FinishDecoding();
            if (!finishResult.Success)
            {
                FailReceiverWithProtocolError(
                    finishResult.FailureMessage,
                    false,
                    0);
            }
        }

        ThrowForReceiverCancellation(cancellationToken);
        Destination.Write(decodedPayload, 0, decodeResult.BytesWritten);
        ThrowForReceiverCancellation(cancellationToken);
        return decodeResult.BytesWritten;
    }

    internal TftpReceiverResult ProcessDallyPacket(
        TftpPacket packet,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        TftpReceiverResult dallyStatus =
            EvaluateDallyTimer(cancellationToken);
        if (dallyStatus.Action == TftpReceiverAction.Completed)
        {
            return dallyStatus;
        }

        if (packet.BlockNumber != FinalBlockNumber ||
            payload.Length != FinalDataLength)
        {
            FailReceiverWithProtocolError(
                "The receiver received unexpected DATA while dallying.",
                false,
                0);
        }

        TftpPacket? finalAcknowledgement = RetainedPacket;
        if (finalAcknowledgement == null)
        {
            Failed = true;
            throw new TftpProtocolException(
                "The receiver has no final ACK available while dallying.",
                false,
                0);
        }

        DuplicatePacketCount = DuplicatePacketCount + 1;
        DallyStartedMilliseconds =
            RetransmissionTimer.TimeSource.GetTimestampMilliseconds();
        return CreateReceiverResult(
            TftpReceiverAction.ReacknowledgeDuplicateData,
            finalAcknowledgement,
            0,
            true);
    }

    internal TftpReceiverResult EvaluateReceiverTimer(
        CancellationToken cancellationToken)
    {
        ThrowForReceiverCancellation(cancellationToken);
        EnsureActiveReceiver();
        if (Dallying)
        {
            return EvaluateDallyTimer(cancellationToken);
        }

        TftpRetransmissionResult timerResult =
            RetransmissionTimer.EvaluateTimer();
        if (timerResult.Action == TftpRetransmissionAction.Wait)
        {
            return CreateReceiverResult(
                TftpReceiverAction.Wait,
                null,
                timerResult.WaitMilliseconds,
                false);
        }

        if (timerResult.Action == TftpRetransmissionAction.Retransmit)
        {
            TftpPacket? retainedPacket = RetainedPacket;
            if (retainedPacket == null)
            {
                Failed = true;
                throw new TftpProtocolException(
                    "The receiver has no packet available for retransmission.",
                    false,
                    0);
            }

            return CreateReceiverResult(
                TftpReceiverAction.RetransmitPacket,
                retainedPacket,
                0,
                false);
        }

        Failed = true;
        throw RetransmissionTimer.CreateTimeoutFailure(timerResult);
    }

    internal TftpReceiverResult EvaluateDallyTimer(
        CancellationToken cancellationToken)
    {
        ThrowForReceiverCancellation(cancellationToken);
        long now = RetransmissionTimer.TimeSource.GetTimestampMilliseconds();
        long totalElapsed = RetransmissionTimer.GetNonnegativeElapsed(
            RetransmissionTimer.TransferStartedMilliseconds,
            now);
        if (totalElapsed >= RetransmissionTimer.MaximumDurationMilliseconds)
        {
            FailReceiverForTotalDuration(totalElapsed);
        }

        long dallyElapsed = RetransmissionTimer.GetNonnegativeElapsed(
            DallyStartedMilliseconds,
            now);
        if (dallyElapsed >= DallyDurationMilliseconds)
        {
            Dallying = false;
            Completed = true;
            RetainedPacket = null;
            return CreateReceiverResult(
                TftpReceiverAction.Completed,
                null,
                0,
                true);
        }

        long waitMilliseconds = DallyDurationMilliseconds - dallyElapsed;
        long totalRemaining =
            RetransmissionTimer.MaximumDurationMilliseconds - totalElapsed;
        if (waitMilliseconds > totalRemaining)
        {
            waitMilliseconds = totalRemaining;
        }

        return CreateReceiverResult(
            TftpReceiverAction.Wait,
            null,
            waitMilliseconds,
            true);
    }

    internal void EnsureActiveReceiver()
    {
        if (!Started)
        {
            throw new TftpProtocolException(
                "The receiver state machine has not started.",
                false,
                0);
        }

        if (Completed)
        {
            throw new TftpProtocolException(
                "The receiver transfer has already completed.",
                false,
                0);
        }

        if (Failed)
        {
            throw new TftpProtocolException(
                "The receiver transfer has already failed.",
                false,
                0);
        }

        if (Cancelled)
        {
            throw new TftpProtocolException(
                "The receiver transfer has already been cancelled.",
                false,
                0);
        }
    }

    internal void ThrowForReceiverCancellation(
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Cancelled = true;
            throw new OperationCanceledException(cancellationToken);
        }
    }

    internal void FailReceiverWithProtocolError(
        string message,
        bool hasPacketOffset,
        int packetOffset)
    {
        Failed = true;
        throw new TftpProtocolException(
            message,
            hasPacketOffset,
            packetOffset);
    }

    internal void FailReceiverForTotalDuration(long elapsedMilliseconds)
    {
        Failed = true;
        TftpRetransmissionResult totalFailure =
            RetransmissionTimer.CreateTimerResult(
                TftpRetransmissionAction.TotalDurationReached,
                0,
                elapsedMilliseconds);
        throw RetransmissionTimer.CreateTimeoutFailure(totalFailure);
    }

    internal TftpReceiverResult CreateReceiverResult(
        TftpReceiverAction action,
        TftpPacket? packet,
        long waitMilliseconds,
        bool isFinalData)
    {
        TftpReceiverResult result = new TftpReceiverResult();
        result.Action = action;
        result.Packet = packet;
        result.WaitMilliseconds = waitMilliseconds;
        result.IsFinalData = isFinalData;
        result.IsDallying = Dallying;
        result.BytesTransferred = BytesTransferred;
        result.EncodedBytesTransferred = EncodedBytesTransferred;
        result.RetransmissionCount =
            RetransmissionTimer.TotalRetransmissionCount;
        result.DuplicatePacketCount = DuplicatePacketCount;
        return result;
    }
}
