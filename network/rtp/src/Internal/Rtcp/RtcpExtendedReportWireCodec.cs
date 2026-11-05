using System;

namespace rtp;

internal static class RtcpExtendedReportWireCodec
{
    const int SenderSourceIdentifierSize = 4;
    const int BlockHeaderSize = 4;

    internal static string? GetExtendedReportValidationError(
        RtcpExtendedReport? report)
    {
        if (report == null)
        {
            return "The RTCP extended-report body is required.";
        }

        if (report.Blocks == null)
        {
            return "The RTCP extended-report block array is required.";
        }

        int blockIndex = 0;
        while (blockIndex < report.Blocks.Length)
        {
            RtcpExtendedReportBlock? block = report.Blocks[blockIndex];
            if (block == null)
            {
                return "An RTCP extended-report block is required.";
            }

            string? blockError = GetExtendedReportBlockValidationError(block);
            if (blockError != null)
            {
                return blockError;
            }

            blockIndex = blockIndex + 1;
        }

        return null;
    }

    internal static string? GetExtendedReportBlockValidationError(
        RtcpExtendedReportBlock block)
    {
        if (!IsRecognizedBlockType(block.Type))
        {
            return "The typed RTCP extended-report block discriminator is invalid.";
        }

        if (block.Type != RtcpExtendedReportBlockType.Unknown &&
            block.WireType != 0 &&
            block.WireType != (byte)block.Type)
        {
            return "The typed RTCP extended-report block discriminator does not match its wire type.";
        }

        if (block.Data == null)
        {
            return "The RTCP extended-report block data is required.";
        }

        if (IsPacketByPacketBlockType(block.Type))
        {
            return RtcpExtendedReportPacketByPacketWireCodec.
                GetPacketByPacketValidationError(block);
        }

        if (IsMetricsBlockType(block.Type))
        {
            return RtcpExtendedReportMetricsWireCodec.
                GetMetricsBlockValidationError(block);
        }

        if (block.Data.Length % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "RTCP extended-report block data must be word-aligned.";
        }

        if (block.Data.Length / RtcpProtocolConstants.LengthWordSize >
            ushort.MaxValue)
        {
            return "The RTCP extended-report block exceeds its wire length limit.";
        }

        return null;
    }

    internal static string? GetExtendedReportDecodeError(ReadOnlySpan<byte> body)
    {
        if (body.Length < SenderSourceIdentifierSize)
        {
            return "The RTCP extended report is missing its sender SSRC.";
        }

        int blockBytes = body.Length - SenderSourceIdentifierSize;
        if (blockBytes % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "The RTCP extended-report block region must be word-aligned.";
        }

        int offset = SenderSourceIdentifierSize;
        while (offset < body.Length)
        {
            int blockLengthInWordsMinusOne = NetworkBinaryReader.ReadUInt16(
                body,
                offset + 2);
            int blockSize = (blockLengthInWordsMinusOne + 1) *
                RtcpProtocolConstants.LengthWordSize;
            if (blockSize > body.Length - offset)
            {
                return "An RTCP extended-report block is truncated according to its length field.";
            }

            byte wireType = NetworkBinaryReader.ReadByte(body, offset);
            if (wireType >= RtcpExtendedReportBlockTypes.LossRunLength &&
                wireType <= RtcpExtendedReportBlockTypes.PacketReceiptTimes)
            {
                string? packetByPacketError =
                    RtcpExtendedReportPacketByPacketWireCodec.
                        GetPacketByPacketDecodeError(
                            wireType,
                            NetworkBinaryReader.ReadByte(body, offset + 1),
                            body.Slice(
                                offset + BlockHeaderSize,
                                blockSize - BlockHeaderSize));
                if (packetByPacketError != null)
                {
                    return packetByPacketError;
                }
            }
            else if (wireType >=
                RtcpExtendedReportBlockTypes.ReceiverReferenceTime &&
                wireType <= RtcpExtendedReportBlockTypes.VoiceOverIpMetrics)
            {
                string? metricsError = RtcpExtendedReportMetricsWireCodec.
                    GetMetricsBlockDecodeError(
                        wireType,
                        body.Slice(
                            offset + BlockHeaderSize,
                            blockSize - BlockHeaderSize));
                if (metricsError != null)
                {
                    return metricsError;
                }
            }

            offset = offset + blockSize;
        }

        return null;
    }

    internal static long CalculateExtendedReportBodySize(
        RtcpExtendedReport report)
    {
        long size = SenderSourceIdentifierSize;
        int blockIndex = 0;
        while (blockIndex < report.Blocks.Length)
        {
            RtcpExtendedReportBlock block = report.Blocks[blockIndex];
            size = size + BlockHeaderSize + GetExtendedReportBlockDataSize(block);
            blockIndex = blockIndex + 1;
        }

        return size;
    }

    internal static RtcpExtendedReport DecodeExtendedReport(
        ReadOnlySpan<byte> body)
    {
        RtcpExtendedReport report = new RtcpExtendedReport();
        report.SenderSynchronizationSource = NetworkBinaryReader.ReadUInt32(body, 0);
        int blockCount = CountExtendedReportBlocks(body);
        report.Blocks = new RtcpExtendedReportBlock[blockCount];
        int blockIndex = 0;
        int offset = SenderSourceIdentifierSize;
        while (offset < body.Length)
        {
            byte wireType = NetworkBinaryReader.ReadByte(body, offset);
            byte typeSpecific = NetworkBinaryReader.ReadByte(body, offset + 1);
            ushort blockLengthInWordsMinusOne = NetworkBinaryReader.ReadUInt16(
                body,
                offset + 2);
            int dataSize = blockLengthInWordsMinusOne *
                RtcpProtocolConstants.LengthWordSize;
            RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
            block.Type = GetExtendedReportBlockType(wireType);
            block.WireType = wireType;
            block.TypeSpecific = typeSpecific;
            block.BlockLengthInWordsMinusOne = blockLengthInWordsMinusOne;
            block.Data = body.Slice(offset + BlockHeaderSize, dataSize).ToArray();
            if (wireType >= RtcpExtendedReportBlockTypes.LossRunLength &&
                wireType <= RtcpExtendedReportBlockTypes.PacketReceiptTimes)
            {
                RtcpExtendedReportPacketByPacketWireCodec.
                    DecodePacketByPacketBlock(block, block.Data);
            }
            else if (wireType >=
                RtcpExtendedReportBlockTypes.ReceiverReferenceTime &&
                wireType <= RtcpExtendedReportBlockTypes.VoiceOverIpMetrics)
            {
                if (wireType == RtcpExtendedReportBlockTypes.StatisticsSummary &&
                    RtcpExtendedReportMetricsWireCodec.
                        ShouldIgnoreStatisticsSummary(
                            typeSpecific,
                            block.Data))
                {
                    block.Type = RtcpExtendedReportBlockType.Unknown;
                }
                else
                {
                    RtcpExtendedReportMetricsWireCodec.DecodeMetricsBlock(
                        block,
                        block.Data);
                }
            }

            report.Blocks[blockIndex] = block;
            blockIndex = blockIndex + 1;
            offset = offset + BlockHeaderSize + dataSize;
        }

        return report;
    }

    internal static void EncodeExtendedReport(
        RtcpExtendedReport report,
        Span<byte> destination)
    {
        NetworkBinaryWriter.WriteUInt32(
            destination,
            0,
            report.SenderSynchronizationSource);
        int blockIndex = 0;
        int offset = SenderSourceIdentifierSize;
        while (blockIndex < report.Blocks.Length)
        {
            RtcpExtendedReportBlock block = report.Blocks[blockIndex];
            int dataSize = GetExtendedReportBlockDataSize(block);
            NetworkBinaryWriter.WriteByte(
                destination,
                offset,
                GetExtendedReportBlockWireType(block));
            NetworkBinaryWriter.WriteByte(
                destination,
                offset + 1,
                GetExtendedReportBlockTypeSpecific(block));
            NetworkBinaryWriter.WriteUInt16(
                destination,
                offset + 2,
                (ushort)(dataSize /
                    RtcpProtocolConstants.LengthWordSize));
            if (IsPacketByPacketBlockType(block.Type))
            {
                RtcpExtendedReportPacketByPacketWireCodec.
                    EncodePacketByPacketData(
                        block,
                        destination.Slice(offset + BlockHeaderSize, dataSize));
            }
            else if (IsMetricsBlockType(block.Type))
            {
                RtcpExtendedReportMetricsWireCodec.EncodeMetricsBlockData(
                    block,
                    destination.Slice(offset + BlockHeaderSize, dataSize));
            }
            else
            {
                NetworkBinaryWriter.WriteBytes(
                    destination,
                    offset + BlockHeaderSize,
                    block.Data);
            }

            offset = offset + BlockHeaderSize + dataSize;
            blockIndex = blockIndex + 1;
        }
    }

    internal static RtcpExtendedReportBlockType GetExtendedReportBlockType(
        byte wireType)
    {
        if (wireType >= RtcpExtendedReportBlockTypes.LossRunLength &&
            wireType <= RtcpExtendedReportBlockTypes.VoiceOverIpMetrics)
        {
            return (RtcpExtendedReportBlockType)wireType;
        }

        return RtcpExtendedReportBlockType.Unknown;
    }

    internal static byte GetExtendedReportBlockWireType(
        RtcpExtendedReportBlock block)
    {
        if (block.Type != RtcpExtendedReportBlockType.Unknown)
        {
            return (byte)block.Type;
        }

        return block.WireType;
    }

    internal static bool IsRecognizedBlockType(RtcpExtendedReportBlockType type)
    {
        return type == RtcpExtendedReportBlockType.Unknown ||
            (type >= RtcpExtendedReportBlockType.LossRunLength &&
            type <= RtcpExtendedReportBlockType.VoiceOverIpMetrics);
    }

    internal static bool IsPacketByPacketBlockType(
        RtcpExtendedReportBlockType type)
    {
        return type == RtcpExtendedReportBlockType.LossRunLength ||
            type == RtcpExtendedReportBlockType.DuplicateRunLength ||
            type == RtcpExtendedReportBlockType.PacketReceiptTimes;
    }

    internal static bool IsMetricsBlockType(RtcpExtendedReportBlockType type)
    {
        return type >= RtcpExtendedReportBlockType.ReceiverReferenceTime &&
            type <= RtcpExtendedReportBlockType.VoiceOverIpMetrics;
    }

    internal static int GetExtendedReportBlockDataSize(
        RtcpExtendedReportBlock block)
    {
        if (IsPacketByPacketBlockType(block.Type))
        {
            return RtcpExtendedReportPacketByPacketWireCodec.
                CalculatePacketByPacketDataSize(block);
        }

        if (IsMetricsBlockType(block.Type))
        {
            return RtcpExtendedReportMetricsWireCodec.
                CalculateMetricsBlockDataSize(block);
        }

        return block.Data.Length;
    }

    internal static byte GetExtendedReportBlockTypeSpecific(
        RtcpExtendedReportBlock block)
    {
        if (IsPacketByPacketBlockType(block.Type))
        {
            return RtcpExtendedReportPacketByPacketWireCodec.
                GetPacketByPacketTypeSpecific(block);
        }

        if (IsMetricsBlockType(block.Type))
        {
            return RtcpExtendedReportMetricsWireCodec.
                GetMetricsBlockTypeSpecific(block);
        }

        return block.TypeSpecific;
    }

    internal static int CountExtendedReportBlocks(ReadOnlySpan<byte> body)
    {
        int count = 0;
        int offset = SenderSourceIdentifierSize;
        while (offset < body.Length)
        {
            int dataSize = NetworkBinaryReader.ReadUInt16(body, offset + 2) *
                RtcpProtocolConstants.LengthWordSize;
            offset = offset + BlockHeaderSize + dataSize;
            count = count + 1;
        }

        return count;
    }
}
