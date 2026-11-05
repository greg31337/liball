using System;

namespace rtp;

internal static class RtcpExtendedReportPacketByPacketWireCodec
{
    const int FixedDataSize = 8;
    const int ChunkSize = 2;
    const int ReceiptTimeSize = 4;
    const int MaximumThinning = 15;
    const int MaximumRunLength = 16383;
    const ushort BitVectorMask = 32767;

    internal static string? GetPacketByPacketValidationError(
        RtcpExtendedReportBlock block)
    {
        if (block.Thinning > MaximumThinning)
        {
            return "An RTCP XR thinning value must fit four bits.";
        }

        int reportedPacketCount = GetReportedPacketCount(
            block.BeginSequenceNumber,
            block.EndSequenceNumber,
            block.Thinning);
        if (reportedPacketCount < 0)
        {
            return "An RTCP XR packet-by-packet sequence range cannot cover 65534 or more sequence numbers.";
        }

        if (block.Type == RtcpExtendedReportBlockType.PacketReceiptTimes)
        {
            if (block.ReceiptTimes == null)
            {
                return "The RTCP XR packet receipt-time array is required.";
            }

            if (block.ReceiptTimes.Length != reportedPacketCount)
            {
                return "The RTCP XR packet receipt-time count must match the thinned sequence range.";
            }

            long receiptDataSize = FixedDataSize +
                (long)block.ReceiptTimes.Length * ReceiptTimeSize;
            if (receiptDataSize / RtcpProtocolConstants.LengthWordSize >
                ushort.MaxValue)
            {
                return "The RTCP XR packet receipt-times block exceeds its wire length limit.";
            }

            return null;
        }

        if (block.RunLengthChunks == null)
        {
            return "The RTCP XR run-length chunk array is required.";
        }

        long rleDataSize = FixedDataSize +
            (long)block.RunLengthChunks.Length * ChunkSize;
        if (block.RunLengthChunks.Length % 2 != 0)
        {
            rleDataSize = rleDataSize + ChunkSize;
        }

        if (rleDataSize / RtcpProtocolConstants.LengthWordSize > ushort.MaxValue)
        {
            return "The RTCP XR run-length block exceeds its wire length limit.";
        }

        int representedCount = 0;
        int chunkIndex = 0;
        while (chunkIndex < block.RunLengthChunks.Length)
        {
            RtcpRunLengthChunk? chunk = block.RunLengthChunks[chunkIndex];
            if (chunk == null)
            {
                return "An RTCP XR run-length chunk is required.";
            }

            if (representedCount >= reportedPacketCount)
            {
                return "RTCP XR run-length chunks exceed the thinned sequence range.";
            }

            if (chunk.IsBitVector)
            {
                if (chunk.BitVector > BitVectorMask)
                {
                    return "An RTCP XR bit-vector chunk must fit fifteen bits.";
                }

                int remainingCount = reportedPacketCount - representedCount;
                if (remainingCount < 15)
                {
                    int unusedBitCount = 15 - remainingCount;
                    int unusedBitMask = (1 << unusedBitCount) - 1;
                    if ((chunk.BitVector & unusedBitMask) != 0)
                    {
                        return "Unused RTCP XR bit-vector positions must be zero.";
                    }

                    representedCount = reportedPacketCount;
                }
                else
                {
                    representedCount = representedCount + 15;
                }
            }
            else
            {
                if (chunk.RunLength < 1 ||
                    chunk.RunLength > MaximumRunLength)
                {
                    return "An RTCP XR run length must be between 1 and 16383.";
                }

                if (chunk.RunLength > reportedPacketCount - representedCount)
                {
                    return "An RTCP XR run-length chunk exceeds the thinned sequence range.";
                }

                representedCount = representedCount + chunk.RunLength;
            }

            chunkIndex = chunkIndex + 1;
        }

        if (representedCount != reportedPacketCount)
        {
            return "RTCP XR run-length chunks must cover the thinned sequence range.";
        }

        return null;
    }

    internal static string? GetPacketByPacketDecodeError(
        byte wireType,
        byte typeSpecific,
        ReadOnlySpan<byte> data)
    {
        if (data.Length < FixedDataSize)
        {
            return "An RTCP XR packet-by-packet block is shorter than its fixed fields.";
        }

        byte thinning = (byte)(typeSpecific & MaximumThinning);
        ushort beginSequenceNumber = NetworkBinaryReader.ReadUInt16(data, 4);
        ushort endSequenceNumber = NetworkBinaryReader.ReadUInt16(data, 6);
        int reportedPacketCount = GetReportedPacketCount(
            beginSequenceNumber,
            endSequenceNumber,
            thinning);
        if (reportedPacketCount < 0)
        {
            return "An RTCP XR packet-by-packet sequence range cannot cover 65534 or more sequence numbers.";
        }

        if (wireType == RtcpExtendedReportBlockTypes.PacketReceiptTimes)
        {
            int receiptTimeCount = (data.Length - FixedDataSize) / ReceiptTimeSize;
            if (receiptTimeCount != reportedPacketCount)
            {
                return "The RTCP XR packet receipt-time count does not match the thinned sequence range.";
            }

            return null;
        }

        int chunkCount = (data.Length - FixedDataSize) / ChunkSize;
        int representedCount = 0;
        int chunkIndex = 0;
        while (chunkIndex < chunkCount)
        {
            ushort packedChunk = NetworkBinaryReader.ReadUInt16(
                data,
                FixedDataSize + chunkIndex * ChunkSize);
            if (packedChunk == 0)
            {
                if (chunkIndex != chunkCount - 1)
                {
                    return "An RTCP XR terminating null chunk must be last.";
                }

                chunkIndex = chunkIndex + 1;
                continue;
            }

            if (representedCount >= reportedPacketCount)
            {
                return "RTCP XR chunks exceed the thinned sequence range.";
            }

            if ((packedChunk & 32768) != 0)
            {
                int remainingCount = reportedPacketCount - representedCount;
                ushort bitVector = (ushort)(packedChunk & BitVectorMask);
                if (remainingCount < 15)
                {
                    int unusedBitCount = 15 - remainingCount;
                    int unusedBitMask = (1 << unusedBitCount) - 1;
                    if ((bitVector & unusedBitMask) != 0)
                    {
                        return "Unused RTCP XR bit-vector positions must be zero.";
                    }

                    representedCount = reportedPacketCount;
                }
                else
                {
                    representedCount = representedCount + 15;
                }
            }
            else
            {
                int runLength = packedChunk & MaximumRunLength;
                if (runLength == 0)
                {
                    return "An RTCP XR run-length chunk cannot have a zero length.";
                }

                if (runLength > reportedPacketCount - representedCount)
                {
                    return "An RTCP XR run-length chunk exceeds the thinned sequence range.";
                }

                representedCount = representedCount + runLength;
            }

            chunkIndex = chunkIndex + 1;
        }

        if (representedCount != reportedPacketCount)
        {
            return "RTCP XR chunks do not cover the thinned sequence range.";
        }

        return null;
    }

    internal static int CalculatePacketByPacketDataSize(
        RtcpExtendedReportBlock block)
    {
        if (block.Type == RtcpExtendedReportBlockType.PacketReceiptTimes)
        {
            return FixedDataSize + block.ReceiptTimes.Length * ReceiptTimeSize;
        }

        int dataSize = FixedDataSize +
            block.RunLengthChunks.Length * ChunkSize;
        if (block.RunLengthChunks.Length % 2 != 0)
        {
            dataSize = dataSize + ChunkSize;
        }

        return dataSize;
    }

    internal static void DecodePacketByPacketBlock(
        RtcpExtendedReportBlock block,
        ReadOnlySpan<byte> data)
    {
        block.Thinning = (byte)(block.TypeSpecific & MaximumThinning);
        block.SourceIdentifier = NetworkBinaryReader.ReadUInt32(data, 0);
        block.BeginSequenceNumber = NetworkBinaryReader.ReadUInt16(data, 4);
        block.EndSequenceNumber = NetworkBinaryReader.ReadUInt16(data, 6);
        if (block.Type == RtcpExtendedReportBlockType.PacketReceiptTimes)
        {
            int receiptTimeCount = (data.Length - FixedDataSize) / ReceiptTimeSize;
            block.ReceiptTimes = new uint[receiptTimeCount];
            int receiptTimeIndex = 0;
            while (receiptTimeIndex < receiptTimeCount)
            {
                block.ReceiptTimes[receiptTimeIndex] = NetworkBinaryReader.ReadUInt32(
                    data,
                    FixedDataSize + receiptTimeIndex * ReceiptTimeSize);
                receiptTimeIndex = receiptTimeIndex + 1;
            }

            return;
        }

        int packedChunkCount = (data.Length - FixedDataSize) / ChunkSize;
        int chunkCount = packedChunkCount;
        if (packedChunkCount != 0 &&
            NetworkBinaryReader.ReadUInt16(
                data,
                FixedDataSize + (packedChunkCount - 1) * ChunkSize) == 0)
        {
            chunkCount = chunkCount - 1;
        }

        block.RunLengthChunks = new RtcpRunLengthChunk[chunkCount];
        int chunkIndex = 0;
        while (chunkIndex < chunkCount)
        {
            ushort packedChunk = NetworkBinaryReader.ReadUInt16(
                data,
                FixedDataSize + chunkIndex * ChunkSize);
            RtcpRunLengthChunk chunk = new RtcpRunLengthChunk();
            if ((packedChunk & 32768) != 0)
            {
                chunk.IsBitVector = true;
                chunk.BitVector = (ushort)(packedChunk & BitVectorMask);
            }
            else
            {
                chunk.PositiveRun = (packedChunk & 16384) != 0;
                chunk.RunLength = packedChunk & MaximumRunLength;
            }

            block.RunLengthChunks[chunkIndex] = chunk;
            chunkIndex = chunkIndex + 1;
        }
    }

    internal static byte GetPacketByPacketTypeSpecific(
        RtcpExtendedReportBlock block)
    {
        return block.Thinning;
    }

    internal static void EncodePacketByPacketData(
        RtcpExtendedReportBlock block,
        Span<byte> destination)
    {
        NetworkBinaryWriter.WriteUInt32(destination, 0, block.SourceIdentifier);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            4,
            block.BeginSequenceNumber);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            6,
            block.EndSequenceNumber);
        if (block.Type == RtcpExtendedReportBlockType.PacketReceiptTimes)
        {
            int receiptTimeIndex = 0;
            while (receiptTimeIndex < block.ReceiptTimes.Length)
            {
                NetworkBinaryWriter.WriteUInt32(
                    destination,
                    FixedDataSize + receiptTimeIndex * ReceiptTimeSize,
                    block.ReceiptTimes[receiptTimeIndex]);
                receiptTimeIndex = receiptTimeIndex + 1;
            }

            return;
        }

        int chunkIndex = 0;
        while (chunkIndex < block.RunLengthChunks.Length)
        {
            RtcpRunLengthChunk chunk = block.RunLengthChunks[chunkIndex];
            ushort packedChunk;
            if (chunk.IsBitVector)
            {
                packedChunk = (ushort)(32768 | chunk.BitVector);
            }
            else
            {
                packedChunk = (ushort)chunk.RunLength;
                if (chunk.PositiveRun)
                {
                    packedChunk = (ushort)(packedChunk | 16384);
                }
            }

            NetworkBinaryWriter.WriteUInt16(
                destination,
                FixedDataSize + chunkIndex * ChunkSize,
                packedChunk);
            chunkIndex = chunkIndex + 1;
        }

        if (block.RunLengthChunks.Length % 2 != 0)
        {
            NetworkBinaryWriter.WriteUInt16(
                destination,
                FixedDataSize + block.RunLengthChunks.Length * ChunkSize,
                0);
        }
    }

    internal static int GetReportedPacketCount(
        ushort beginSequenceNumber,
        ushort endSequenceNumber,
        byte thinning)
    {
        int sequenceCount = (endSequenceNumber - beginSequenceNumber) & 65535;
        if (sequenceCount >= 65534)
        {
            return -1;
        }

        int step = 1 << thinning;
        int stepMask = step - 1;
        int firstOffset = (step - (beginSequenceNumber & stepMask)) & stepMask;
        if (firstOffset >= sequenceCount)
        {
            return 0;
        }

        return 1 + (sequenceCount - 1 - firstOffset) / step;
    }
}
